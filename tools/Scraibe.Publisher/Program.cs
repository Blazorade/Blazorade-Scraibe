using Scraibe.Publisher;

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
    AssemblyPath:         AbsPath(cwd, Require(args, "--assembly")),
    ComponentNamespace:   Require(args, "--component-namespace"),
    LayoutsPath:          AbsPath(cwd, Require(args, "--layouts")),
    ExcludedPaths:        All(args, "--excluded"),
    BlazorScript:         blazorScript
);

Console.WriteLine($"Scraibe Publisher");
Console.WriteLine($"  Content : {opts.ContentPath}");
Console.WriteLine($"  Output  : {opts.OutputPath}");
Console.WriteLine($"  Host    : {opts.HostName}");

// ── Load component registry ──────────────────────────────────────────────────

var registry = new ComponentRegistry(opts.AssemblyPath, opts.ComponentNamespace);

// ── Walk content tree ─────────────────────────────────────────────────────────

Console.WriteLine("\nScanning content...");
var allMdFiles = Directory.GetFiles(opts.ContentPath, "*.md", SearchOption.AllDirectories);

// Apply exclusions
bool IsExcluded(string absPath)
{
    var rel = Path.GetRelativePath(opts.ContentPath, absPath).Replace('\\', '/');
    return opts.ExcludedPaths.Any(ex =>
        rel.Equals(ex, StringComparison.OrdinalIgnoreCase) ||
        rel.StartsWith(ex.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase));
}

// Skip _name.md part files from the page manifest — they are resolved per-page
bool IsPartFile(string absPath)
    => Path.GetFileName(absPath).StartsWith('_');

var candidates = allMdFiles
    .Where(f => !IsExcluded(f) && !IsPartFile(f))
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

// ── Stale file cleanup ───────────────────────────────────────────────────────

Console.WriteLine("\nCleaning up stale files...");
var publishedOutputs = new HashSet<string>(
    published.Select(p => p.OutputPath), StringComparer.OrdinalIgnoreCase);

var deleted = new List<string>();
foreach (var htmlFile in Directory.GetFiles(opts.OutputPath, "*.html", SearchOption.AllDirectories))
{
    var name = Path.GetFileName(htmlFile);
    if (name.Equals("index.html", StringComparison.OrdinalIgnoreCase)) continue;
    if (!publishedOutputs.Contains(htmlFile))
    {
        File.Delete(htmlFile);
        deleted.Add(Path.GetRelativePath(opts.OutputPath, htmlFile));
    }
}

// Prune empty subdirectories (bottom-up)
foreach (var dir in Directory.GetDirectories(opts.OutputPath, "*", SearchOption.AllDirectories)
    .OrderByDescending(d => d.Length))
{
    if (!Directory.EnumerateFileSystemEntries(dir).Any())
        Directory.Delete(dir);
}

if (deleted.Count > 0)
    foreach (var d in deleted) Console.WriteLine($"  🗑  deleted: {d}");
else
    Console.WriteLine("  No stale files.");

// ── Summary ───────────────────────────────────────────────────────────────────

Console.WriteLine($"""

── Publish summary ──────────────────────────────────
  Pages published : {published.Count}
  Errors          : {errors.Count}
  Stale deleted   : {deleted.Count}
─────────────────────────────────────────────────────
""");

if (errors.Count > 0)
{
    Console.Error.WriteLine("Errors:");
    foreach (var e in errors)
        Console.Error.WriteLine($"  {e.Page.Slug}: {e.Error}");
    Environment.Exit(1);
}
