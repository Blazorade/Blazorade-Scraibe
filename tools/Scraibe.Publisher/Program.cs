using Scraibe.Publisher;
using System.Text.Json;
using System.Text.Json.Nodes;

// ── Argument parsing ─────────────────────────────────────────────────────────

static string Require(string[] args, string flag)
{
    var idx = Array.IndexOf(args, flag);
    if (idx < 0 || idx + 1 >= args.Length)
    {
        Console.Error.WriteLine($"Error: missing required argument {flag}");
        Environment.Exit(1);
    }
    return args[idx + 1];
}


static List<string> All(string[] args, string flag)
{
    var result = new List<string>();
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == flag) result.Add(args[i + 1]);
    return result;
}

static string AbsPath(string root, string path)
    => Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(root, path));

var cwd        = Directory.GetCurrentDirectory();
var outputPath = AbsPath(cwd, Require(args, "--output"));

// Extract the fingerprinted Blazor script src from wwwroot/index.html so every
// published page can boot the Blazor runtime at its clean URL.
var blazorScript  = "_framework/blazor.webassembly.js"; // fallback
var indexHtmlPath = Path.Combine(outputPath, "index.html");
if (File.Exists(indexHtmlPath))
{
    var indexContent = File.ReadAllText(indexHtmlPath);
    var scriptMatch  = System.Text.RegularExpressions.Regex.Match(
        indexContent,
        @"<script[^>]*\ssrc=""(_framework/blazor\.webassembly[^""]+)""",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    if (scriptMatch.Success)
        blazorScript = scriptMatch.Groups[1].Value;
}

var opts = new PublishOptions(
    ContentPath:          AbsPath(cwd, Require(args, "--content")),
    OutputPath:           outputPath,
    HostName:             Require(args, "--host"),
    DisplayName:          Require(args, "--display-name"),
    TemplatePath:         AbsPath(cwd, Require(args, "--template")),
    StaticWebAppTemplatePath: AbsPath(cwd, Require(args, "--staticwebapp-template")),
    AssemblyPath:         AbsPath(cwd, Require(args, "--assembly")),
    ComponentNamespace:   Require(args, "--component-namespace"),
    LayoutsPath:          AbsPath(cwd, Require(args, "--layouts")),
    ExcludedPaths:        All(args, "--excluded"),
    BlazorScript:         blazorScript
);

static bool IsContentExcluded(PublishOptions options, string absPath)
{
    var rel = Path.GetRelativePath(options.ContentPath, absPath).Replace('\\', '/');
    return options.ExcludedPaths.Any(ex =>
        rel.Equals(ex, StringComparison.OrdinalIgnoreCase) ||
        rel.StartsWith(ex.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase));
}

static bool IsEligibleAssetFileName(string fileName)
{
    if (string.IsNullOrWhiteSpace(fileName)) return false;
    return char.IsLetterOrDigit(fileName[0])
        && !fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
}

static IEnumerable<string> EnumerateEligibleAssetRelativePaths(PublishOptions options)
{
    foreach (var sourceFile in Directory.GetFiles(options.ContentPath, "*", SearchOption.AllDirectories))
    {
        if (IsContentExcluded(options, sourceFile)) continue;

        var fileName = Path.GetFileName(sourceFile);
        if (!IsEligibleAssetFileName(fileName)) continue;

        yield return Path.GetRelativePath(options.ContentPath, sourceFile).Replace('\\', '/');
    }
}

static HashSet<string> DiscoverEligibleAssetFolderExcludes(PublishOptions options)
{
    var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var rel in EnumerateEligibleAssetRelativePaths(options))
    {
        var folder = Path.GetDirectoryName(rel)?.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(folder))
        {
            patterns.Add($"/{Path.GetFileName(rel)}");
        }
        else
        {
            patterns.Add($"/{folder}/*");
        }
    }
    return patterns;
}

static (string Route, string Rewrite) ToRouteRewrite(PageInfo page)
{
    var slug = page.Slug.Replace('\\', '/').Trim('/');
    var route = slug.Equals("home", StringComparison.OrdinalIgnoreCase)
        ? "/"
        : slug.EndsWith("/home", StringComparison.OrdinalIgnoreCase)
            ? "/" + slug[..^5]
            : "/" + slug;

    route = string.IsNullOrWhiteSpace(route) ? "/" : route;
    var rewrite = "/" + slug + ".html";

    return (route, rewrite);
}

static JsonObject CreateRouteNode(string route, string rewrite)
    => new()
    {
        ["route"] = route,
        ["rewrite"] = rewrite
    };

static JsonObject ParseJsonObject(string json)
    => JsonNode.Parse(json) as JsonObject ?? new JsonObject();

static JsonObject LoadStaticWebAppConfigBase(PublishOptions options, bool useTemplateBase)
{
    var outputConfigPath = Path.Combine(options.OutputPath, "staticwebapp.config.json");
    if (useTemplateBase && File.Exists(options.StaticWebAppTemplatePath))
    {
        return ParseJsonObject(File.ReadAllText(options.StaticWebAppTemplatePath));
    }

    if (File.Exists(outputConfigPath))
    {
        return ParseJsonObject(File.ReadAllText(outputConfigPath));
    }

    return new JsonObject
    {
        ["navigationFallback"] = new JsonObject
        {
            ["rewrite"] = "/index.html",
            ["exclude"] = new JsonArray()
        }
    };
}

static int UpsertRoutes(JsonObject root, IEnumerable<PageInfo> pages, bool replaceAll)
{
    var routes = root["routes"] as JsonArray ?? new JsonArray();
    root["routes"] = routes;

    var byRoute = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
    foreach (var item in routes.OfType<JsonObject>())
    {
        var key = item["route"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(key))
        {
            byRoute[key] = item;
        }
    }

    if (replaceAll)
    {
        routes.Clear();
        byRoute.Clear();
    }

    var changed = 0;
    foreach (var page in pages)
    {
        var (route, rewrite) = ToRouteRewrite(page);
        if (byRoute.TryGetValue(route, out var existing))
        {
            var currentRewrite = existing["rewrite"]?.GetValue<string>() ?? "";
            if (!string.Equals(currentRewrite, rewrite, StringComparison.Ordinal))
            {
                existing["rewrite"] = rewrite;
                changed++;
            }
        }
        else
        {
            var node = CreateRouteNode(route, rewrite);
            routes.Add(node);
            byRoute[route] = node;
            changed++;
        }
    }

    return changed;
}

static int MergeNavigationFallbackExclusions(JsonObject root, IEnumerable<string> excludePatterns)
{
    var navigationFallback = root["navigationFallback"] as JsonObject ?? new JsonObject();
    root["navigationFallback"] = navigationFallback;

    if (navigationFallback["exclude"] is not JsonArray excludeArray)
    {
        excludeArray = new JsonArray();
        navigationFallback["exclude"] = excludeArray;
    }

    var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var item in excludeArray)
    {
        if (item is JsonValue value)
        {
            var entry = value.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(entry))
            {
                existing.Add(entry);
            }
        }
    }

    var added = 0;
    foreach (var pattern in excludePatterns
        .Select(e => e.Trim())
        .Where(e => e.Length > 0)
        .Select(e => e.Replace('\\', '/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(e => e, StringComparer.OrdinalIgnoreCase))
    {
        if (existing.Contains(pattern))
        {
            continue;
        }

        excludeArray.Add(pattern);
        existing.Add(pattern);
        added++;
    }

    return added;
}

static void SaveStaticWebAppConfig(PublishOptions options, JsonObject root)
{
    var outputConfigPath = Path.Combine(options.OutputPath, "staticwebapp.config.json");
    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(outputConfigPath, root.ToJsonString(jsonOptions));
}

Console.WriteLine($"Scraibe Publisher");
Console.WriteLine($"  Content : {opts.ContentPath}");
Console.WriteLine($"  Output  : {opts.OutputPath}");
Console.WriteLine($"  Host    : {opts.HostName}");

// ── Load component registry ──────────────────────────────────────────────────

var registry = new ComponentRegistry(opts.AssemblyPath, opts.ComponentNamespace);

// ── Partial publish (--page args supplied) ────────────────────────────────────

var selectedPages = All(args, "--page");

if (selectedPages.Count > 0)
{
    Console.WriteLine($"\nPartial publish: {selectedPages.Count} page(s) specified.");

    // Step 1: resolve source paths
    var partialSources = selectedPages
        .Select(rel => (
            Rel: rel.Replace('\\', '/').TrimStart('/'),
            Src: Path.GetFullPath(Path.Combine(opts.ContentPath,
                     rel.Replace('\\', '/').TrimStart('/')))))
        .ToList();

    // Validate: all source files must exist
    var missingSrc = partialSources.Where(p => !File.Exists(p.Src)).ToList();
    if (missingSrc.Count > 0)
    {
        foreach (var m in missingSrc)
            Console.Error.WriteLine($"Error: source file not found: {m.Src}");
        Environment.Exit(1);
    }

    // Step 4: parse selected .md files into PageInfo (same logic as full publish)
    var partialPageInfos = new List<PageInfo>();
    foreach (var (rel, src) in partialSources)
    {
        var raw = File.ReadAllText(src);
        var (fm, _) = FrontmatterParser.Parse(raw, src);

        var dir  = Path.GetDirectoryName(rel)?.Replace('\\', '/') ?? "";
        var stem = Path.GetFileNameWithoutExtension(rel);
        var slug = fm.Slug ?? stem;
        var fullSlug = string.IsNullOrEmpty(dir) ? slug : $"{dir}/{slug}";

        var outputFile   = Path.Combine(opts.OutputPath,
            fullSlug.Replace('/', Path.DirectorySeparatorChar) + ".html");
        var canonicalUrl = $"https://{opts.HostName}/{fullSlug}.html";
        var lastMod      = File.GetLastWriteTime(src);

        partialPageInfos.Add(new PageInfo(
            SourcePath:   src,
            RelativePath: rel,
            OutputPath:   outputFile,
            Slug:         fullSlug,
            CanonicalUrl: canonicalUrl,
            Frontmatter:  fm,
            LastModified: lastMod));
    }

    // Step 2: pre-flight — all target .html files must already exist
    var missingOut = partialPageInfos.Where(p => !File.Exists(p.OutputPath)).ToList();
    if (missingOut.Count > 0)
    {
        Console.Error.WriteLine(
            "Pre-flight check failed. The following pages have never been published:");
        foreach (var m in missingOut)
            Console.Error.WriteLine(
                $"  Missing: {Path.GetRelativePath(opts.OutputPath, m.OutputPath)}");
        Console.Error.WriteLine("Run a full publish first, then retry the partial publish.");
        Environment.Exit(1);
    }

    // Step 5: extract nav HTML from the first existing output file
    var partialNavHtml = "";
    {
        var existingHtml = File.ReadAllText(partialPageInfos[0].OutputPath);
        var parser       = new AngleSharp.Html.Parser.HtmlParser();
        var doc          = parser.ParseDocument(existingHtml);
        var navEl        = doc.QuerySelector("[x-part='nav']");
        partialNavHtml = navEl?.InnerHtml ?? "";
    }

    // Step 6: publish only the specified pages
    Console.WriteLine("\nPublishing pages...");
    var partialPublished = new List<PageInfo>();
    var partialErrors    = new List<PageResult>();

    foreach (var page in partialPageInfos)
    {
        var result = PagePublisher.Publish(page, registry, opts, partialNavHtml, partialPageInfos);
        if (result.Success)
        {
            Console.WriteLine($"  ✓  {page.Slug}.html");
            partialPublished.Add(page);
        }
        else
        {
            Console.Error.WriteLine($"  ✗  {page.Slug} — {result.Error}");
            partialErrors.Add(result);
        }
    }

    // Step 7: update sitemap.xml in-place
    var sitemapPath = Path.Combine(opts.OutputPath, "sitemap.xml");
    if (File.Exists(sitemapPath))
    {
        SitemapGenerator.Update(partialPublished, sitemapPath, opts.HostName);
        Console.WriteLine($"\n  ✓  sitemap.xml updated ({partialPublished.Count} entries patched).");
    }
    else
    {
        SitemapGenerator.Generate(partialPublished, sitemapPath, opts.HostName);
        Console.WriteLine($"\n  ✓  sitemap.xml generated ({partialPublished.Count} entries).");
    }

    var partialConfig = LoadStaticWebAppConfigBase(opts, useTemplateBase: false);
    var partialRouteUpdates = UpsertRoutes(partialConfig, partialPublished, replaceAll: false);
    var partialAssetFolderExcludes = DiscoverEligibleAssetFolderExcludes(opts);
    var partialAddedExcludes = MergeNavigationFallbackExclusions(partialConfig, partialAssetFolderExcludes);
    if (partialRouteUpdates > 0 || partialAddedExcludes > 0)
    {
        SaveStaticWebAppConfig(opts, partialConfig);
        Console.WriteLine($"\n  ✓  staticwebapp.config.json updated ({partialRouteUpdates} route change(s), {partialAddedExcludes} exclude pattern(s) added).");
    }

    Console.WriteLine($"""

── Publish summary ──────────────────────────────────
  Pages published : {partialPublished.Count} (partial run)
  Errors          : {partialErrors.Count}
─────────────────────────────────────────────────────
""");

    if (partialErrors.Count > 0)
    {
        Console.Error.WriteLine("Errors:");
        foreach (var e in partialErrors)
            Console.Error.WriteLine($"  {e.Page.Slug}: {e.Error}");
        Environment.Exit(1);
    }

    Environment.Exit(0);
}

// ── Walk content tree ─────────────────────────────────────────────────────────

Console.WriteLine("\nScanning content...");
var allMdFiles = Directory.GetFiles(opts.ContentPath, "*.md", SearchOption.AllDirectories);

// Apply exclusions
// Skip _name.md part files from the page manifest — they are resolved per-page
bool IsPartFile(string absPath)
    => Path.GetFileName(absPath).StartsWith('_');

var candidates = allMdFiles
    .Where(f => !IsContentExcluded(opts, f) && !IsPartFile(f))
    .ToList();

// ── Validate: no index.md, no subdir named home, no flat+subdir conflicts ─────

bool fatalError = false;

foreach (var f in candidates)
{
    var name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
    if (name == "index")
    {
        Console.Error.WriteLine($"Blocked: {f} — 'index' is a reserved name. Skipping.");
        candidates.Remove(f);
        break; // restart loop after mutation is unsafe; just skip for now
    }
}
candidates = candidates
    .Where(f => !Path.GetFileNameWithoutExtension(f).Equals("index", StringComparison.OrdinalIgnoreCase))
    .ToList();

// Subdir named "home" check
foreach (var dir in Directory.GetDirectories(opts.ContentPath, "*", SearchOption.AllDirectories))
{
    if (Path.GetFileName(dir).Equals("home", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"Fatal: directory named 'home' found at '{dir}'. " +
            "This is a reserved name. Rename the directory and re-run.");
        fatalError = true;
    }
}

// Flat .md + same-named subdir conflict
foreach (var f in candidates)
{
    var stem    = Path.GetFileNameWithoutExtension(f);
    var sibDir  = Path.Combine(Path.GetDirectoryName(f)!, stem);
    if (Directory.Exists(sibDir))
    {
        Console.Error.WriteLine($"Fatal: flat file '{f}' conflicts with directory '{sibDir}'. " +
            "Remove one of them and re-run.");
        fatalError = true;
    }
}

if (fatalError) { Console.Error.WriteLine("Publish aborted."); Environment.Exit(1); }

// ── Build PageInfo list ───────────────────────────────────────────────────────

var pages = new List<PageInfo>();
foreach (var f in candidates)
{
    var raw = File.ReadAllText(f);
    var (fm, _) = FrontmatterParser.Parse(raw, f);

    var rel  = Path.GetRelativePath(opts.ContentPath, f).Replace('\\', '/');
    var dir  = Path.GetDirectoryName(rel)?.Replace('\\', '/') ?? "";
    var stem = Path.GetFileNameWithoutExtension(rel);

    // Apply slug override from frontmatter
    var slug = fm.Slug ?? stem;

    // Full relative slug = dir + slug
    var fullSlug = string.IsNullOrEmpty(dir) ? slug : $"{dir}/{slug}";

    var outputFile = Path.Combine(opts.OutputPath,
        fullSlug.Replace('/', Path.DirectorySeparatorChar) + ".html");

    var canonicalUrl = $"https://{opts.HostName}/{fullSlug}.html";

    var lastMod = File.GetLastWriteTime(f);

    pages.Add(new PageInfo(
        SourcePath:   f,
        RelativePath: rel,
        OutputPath:   outputFile,
        Slug:         fullSlug,
        CanonicalUrl: canonicalUrl,
        Frontmatter:  fm,
        LastModified: lastMod
    ));
}

Console.WriteLine($"  {pages.Count} page(s) to publish.");

// ── Generate shared navbar ────────────────────────────────────────────────────

var navHtml = NavGenerator.Generate(pages, opts.DisplayName);

// ── Publish each page ─────────────────────────────────────────────────────────

Console.WriteLine("\nPublishing pages...");
var published = new List<PageInfo>();
var errors    = new List<PageResult>();

foreach (var page in pages)
{
    var result = PagePublisher.Publish(page, registry, opts, navHtml, pages);
    if (result.Success)
    {
        Console.WriteLine($"  ✓  {page.Slug}.html");
        published.Add(page);
    }
    else
    {
        Console.Error.WriteLine($"  ✗  {page.Slug} — {result.Error}");
        errors.Add(result);
    }
}

// ── Sitemap ───────────────────────────────────────────────────────────────────

var sitemapOutput = Path.Combine(opts.OutputPath, "sitemap.xml");

SitemapGenerator.Generate(published, sitemapOutput, opts.HostName);
Console.WriteLine($"\n  ✓  sitemap.xml updated ({published.Count} entries).");

// ── Static asset sync ────────────────────────────────────────────────────────

Console.WriteLine("\nSyncing static assets...");

var copiedAssets     = 0;

foreach (var sourceFile in Directory.GetFiles(opts.ContentPath, "*", SearchOption.AllDirectories))
{
    if (IsContentExcluded(opts, sourceFile)) continue;

    var fileName = Path.GetFileName(sourceFile);
    if (!IsEligibleAssetFileName(fileName)) continue;

    var relative = Path.GetRelativePath(opts.ContentPath, sourceFile);
    var destination = Path.Combine(opts.OutputPath, relative);
    var destinationDir = Path.GetDirectoryName(destination);
    if (!string.IsNullOrEmpty(destinationDir))
        Directory.CreateDirectory(destinationDir);

    File.Copy(sourceFile, destination, overwrite: true);
    copiedAssets++;
}

Console.WriteLine($"  ✓  static assets synced ({copiedAssets} copied).");

var existingConfig = LoadStaticWebAppConfigBase(opts, useTemplateBase: false);
var existingExcludePatterns = new List<string>();
if (existingConfig["navigationFallback"] is JsonObject existingFallback
    && existingFallback["exclude"] is JsonArray existingExcludes)
{
    foreach (var item in existingExcludes.OfType<JsonValue>())
    {
        var value = item.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(value)) existingExcludePatterns.Add(value);
    }
}

var rebuiltConfig = LoadStaticWebAppConfigBase(opts, useTemplateBase: true);
var routeUpdates = UpsertRoutes(rebuiltConfig, published, replaceAll: true);
var assetFolderExcludes = DiscoverEligibleAssetFolderExcludes(opts);
var preservedExcludeAdds = MergeNavigationFallbackExclusions(rebuiltConfig, existingExcludePatterns);
var assetExcludeAdds = MergeNavigationFallbackExclusions(rebuiltConfig, assetFolderExcludes);
SaveStaticWebAppConfig(opts, rebuiltConfig);
Console.WriteLine($"  ✓  staticwebapp.config.json updated ({routeUpdates} route(s), {assetExcludeAdds} asset folder exclusion(s), {preservedExcludeAdds} preserved exclusion(s) added).");

// ── Stale file cleanup ───────────────────────────────────────────────────────

Console.WriteLine("\nCleaning up stale files...");
var publishedOutputs = new HashSet<string>(
    published.Select(p => p.OutputPath), StringComparer.OrdinalIgnoreCase);

var deletedHtml = new List<string>();

foreach (var htmlFile in Directory.GetFiles(opts.OutputPath, "*.html", SearchOption.AllDirectories))
{
    var name = Path.GetFileName(htmlFile);
    if (name.Equals("index.html", StringComparison.OrdinalIgnoreCase)) continue;
    if (!publishedOutputs.Contains(htmlFile))
    {
        File.Delete(htmlFile);
        deletedHtml.Add(Path.GetRelativePath(opts.OutputPath, htmlFile));
    }
}

// Prune empty subdirectories (bottom-up)
foreach (var dir in Directory.GetDirectories(opts.OutputPath, "*", SearchOption.AllDirectories)
    .OrderByDescending(d => d.Length))
{
    if (!Directory.EnumerateFileSystemEntries(dir).Any())
        Directory.Delete(dir);
}

if (deletedHtml.Count == 0)
    Console.WriteLine("  No stale files.");
else
{
    foreach (var d in deletedHtml)
        Console.WriteLine($"  🗑  deleted html: {d}");
}

// ── Summary ───────────────────────────────────────────────────────────────────

Console.WriteLine($"""

── Publish summary ──────────────────────────────────
  Pages published : {published.Count}
  Assets copied   : {copiedAssets}
  Errors          : {errors.Count}
    Stale deleted   : {deletedHtml.Count}
─────────────────────────────────────────────────────
""");

if (errors.Count > 0)
{
    Console.Error.WriteLine("Errors:");
    foreach (var e in errors)
        Console.Error.WriteLine($"  {e.Page.Slug}: {e.Error}");
    Environment.Exit(1);
}
