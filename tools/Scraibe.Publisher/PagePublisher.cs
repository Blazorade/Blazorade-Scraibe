using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Markdig;

namespace Scraibe.Publisher;

/// <summary>
/// Orchestrates the publish pipeline for a single page:
///   read markdown → parse frontmatter → resolve parts → process shortcodes
///   → convert to HTML → apply template → write output file.
/// </summary>
static class PagePublisher
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// Publishes one page. Returns a <see cref="PageResult"/> — never throws.
    /// The caller provides pre-built nav HTML so every page gets the same navbar.
    /// </summary>
    public static PageResult Publish(
        PageInfo page,
        ComponentRegistry registry,
        PublishOptions opts,
        string navHtml,
        IReadOnlyList<PageInfo> allPages)
    {
        try
        {
            // 1. Read source fresh from disk
            var raw = File.ReadAllText(page.SourcePath);

            // 2. Frontmatter (already parsed into page.Frontmatter, but body is re-extracted here)
            var (_, body) = FrontmatterParser.Parse(raw, page.SourcePath);

            // 3. Verify layout exists
            var layoutFile = Path.Combine(opts.LayoutsPath, page.Frontmatter.Layout + ".html");
            if (!File.Exists(layoutFile))
            {
                // Case-insensitive fallback
                var match = Directory.GetFiles(opts.LayoutsPath, "*.html")
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                        .Equals(page.Frontmatter.Layout, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                    return new PageResult(page, false,
                        $"Layout '{page.Frontmatter.Layout}' not found in '{opts.LayoutsPath}'.");
            }

            // 4. Resolve _name.md scoped parts (walk up from page's dir to /content)
            var scopedParts = ResolveScopedParts(page, opts, registry, allPages);

            // 5. Process shortcodes (returns processed markdown + [Part] entries)
            var scResult = ShortcodeProcessor.Process(body, registry, page.SourcePath, opts);
            var inlineParts = scResult.Parts;

            // 6. Convert processed markdown to HTML
            var bodyHtml = Markdig.Markdown.ToHtml(scResult.ProcessedMarkdown, Pipeline);

            // Inject wrapping-shortcode inner HTML now that Markdig has run.
            // This must happen BEFORE PostProcessHtml so that .md links inside shortcode
            // inner content are also rewritten to .html.
            // (Inner HTML was deferred out of ProcessedMarkdown to prevent Markdig's
            // condition-6 HTML blocks from terminating at blank lines inside the content.)
            foreach (var (placeholder, innerHtml) in scResult.ShortcodeInners)
                bodyHtml = bodyHtml.Replace($"<!--{placeholder}-->", innerHtml);

            var contentRelativeDir = Path.GetDirectoryName(page.RelativePath)?.Replace('\\', '/') ?? "";
            bodyHtml = UrlRewriter.Rewrite(bodyHtml, contentRelativeDir);

            bodyHtml = PostProcessHtml(bodyHtml);

            // 7. Merge parts: [Part] shortcodes → scoped _name.md files → page body → nav
            var parts = MergeParts(inlineParts, scopedParts, navHtml, bodyHtml);

            // 8. Apply page template
            var html = ApplyTemplate(page, parts, opts);

            // 9. Write output
            Directory.CreateDirectory(Path.GetDirectoryName(page.OutputPath)!);
            File.WriteAllText(page.OutputPath, html, Encoding.UTF8);

            return new PageResult(page, true);
        }
        catch (PublishException ex)
        {
            return new PageResult(page, false, ex.Message);
        }
        catch (Exception ex)
        {
            return new PageResult(page, false, $"Unexpected error: {ex.Message}");
        }
    }

    // ── Template application ────────────────────────────────────────────────────

    private static readonly HtmlParser HtmlParser = new();

    private static string ApplyTemplate(PageInfo page, List<PartInfo> parts, PublishOptions opts)
    {
        var template = File.ReadAllText(opts.TemplatePath);
        var fm       = page.Frontmatter;
        var lastMod  = fm.Date ?? page.LastModified.ToString("yyyy-MM-dd");

        // Derive clean slug: strip trailing /home, or empty string for the root page.
        var cleanSlug = page.Slug.Equals("home", StringComparison.OrdinalIgnoreCase)
            ? ""
            : page.Slug.EndsWith("/home", StringComparison.OrdinalIgnoreCase)
                ? page.Slug[..^5]
                : page.Slug;

        // Build layout_html by injecting part content into each x-part slot.
        var layoutFile = Path.Combine(opts.LayoutsPath, fm.Layout + ".html");
        if (!File.Exists(layoutFile))
        {
            layoutFile = Directory.GetFiles(opts.LayoutsPath, "*.html")
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                    .Equals(fm.Layout, StringComparison.OrdinalIgnoreCase)) ?? layoutFile;
        }
        var layoutHtml    = File.ReadAllText(layoutFile);
        var layoutDoc     = HtmlParser.ParseDocument(layoutHtml);
        var partsByName   = parts.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var slot in layoutDoc.QuerySelectorAll("[x-part]").ToList())
        {
            var partName = slot.GetAttribute("x-part") ?? "";
            if (partsByName.TryGetValue(partName, out var part))
                slot.InnerHtml = part.InnerHtml;
            else
                slot.Parent?.RemoveChild(slot); // no content → remove slot entirely
        }

        var layoutHtmlResult = layoutDoc.Body?.InnerHtml ?? "";

        // Token substitution
        var result = template;
        result = result.Replace("{title}",         HtmlEncode(fm.Title));
        result = result.Replace("{description}",   HtmlEncode(fm.Description ?? ""));
        result = result.Replace("{slug}",          page.Slug);
        result = result.Replace("{cleanSlug}",     cleanSlug);
        result = result.Replace("{HostName}",      opts.HostName);
        result = result.Replace("{layout}",        fm.Layout);
        result = result.Replace("{date}",          lastMod);
        result = result.Replace("{layout_html}",   layoutHtmlResult);
        result = result.Replace("{blazor_script}", opts.BlazorScript);

        // Optional tokens: remove whole line when field is absent
        result = ReplaceOrRemoveLine(result, "{keywords}", fm.Keywords);
        result = ReplaceOrRemoveLine(result, "{author}",   fm.Author);

        return result;
    }

    private static string ReplaceOrRemoveLine(string template, string token, string? value)
    {
        if (value != null)
            return template.Replace(token, HtmlEncode(value));

        // Remove any line that contains the token
        var lines = template.Split('\n');
        return string.Join('\n', lines.Where(l => !l.Contains(token)));
    }

    // ── Parts resolution ────────────────────────────────────────────────────────

    private static List<PartInfo> ResolveScopedParts(
        PageInfo page, PublishOptions opts, ComponentRegistry registry,
        IReadOnlyList<PageInfo> allPages)
    {
        var result = new Dictionary<string, PartInfo>(StringComparer.OrdinalIgnoreCase);

        // Walk up from page's directory to content root, deepest wins
        var pageDir = Path.GetDirectoryName(page.SourcePath)!;
        var dirs = new List<string>();
        var dir = pageDir;
        while (dir.StartsWith(opts.ContentPath, StringComparison.OrdinalIgnoreCase))
        {
            dirs.Add(dir);
            var parent = Path.GetDirectoryName(dir);
            if (parent == null || parent == dir) break;
            dir = parent;
        }
        dirs.Reverse(); // root first, page's dir last (deepest wins by overwriting)

        foreach (var d in dirs)
        {
            foreach (var mdFile in Directory.GetFiles(d, "_*.md"))
            {
                var stem     = Path.GetFileNameWithoutExtension(mdFile)[1..]; // strip leading _
                var partName = stem.ToLowerInvariant();
                var elemName = ElementNames.For(partName);

                var raw      = File.ReadAllText(mdFile);
                var (fm, body) = FrontmatterParser.Parse(raw, mdFile);
                if (fm.RawFields().TryGetValue("element_name", out var elemOverride))
                    elemName = elemOverride;

                // Reusable parts should follow the same processing path as page main content.
                var scResult = ShortcodeProcessor.Process(body, registry, mdFile, opts);
                var partHtml = Markdig.Markdown.ToHtml(scResult.ProcessedMarkdown, Pipeline);

                foreach (var (placeholder, inner) in scResult.ShortcodeInners)
                    partHtml = partHtml.Replace($"<!--{placeholder}-->", inner);

                var partRelativeDir = Path.GetDirectoryName(
                    Path.GetRelativePath(opts.ContentPath, mdFile))?.Replace('\\', '/') ?? "";

                partHtml = UrlRewriter.Rewrite(partHtml, partRelativeDir);
                partHtml = PostProcessHtml(partHtml);

                var innerHtml = partHtml.Trim();
                result[partName] = new PartInfo(partName, elemName, innerHtml);
            }
        }

        return result.Values.ToList();
    }

    private static List<PartInfo> MergeParts(
        List<PartInfo> inlineParts,
        List<PartInfo> scopedParts,
        string navHtml,
        string bodyHtml)
    {
        // Priority (highest → lowest): [Part] shortcode > _name.md > page body > auto-nav
        var merged = new Dictionary<string, PartInfo>(StringComparer.OrdinalIgnoreCase);

        // Auto-nav (lowest priority)
        merged["nav"]  = new PartInfo("nav",  "nav",     navHtml);

        // Page body becomes the default "main" content
        merged["main"] = new PartInfo("main", "article", bodyHtml);

        // Scoped _name.md files overwrite (including _main.md if present)
        foreach (var p in scopedParts) merged[p.Name] = p;

        // Inline [Part] shortcodes (highest priority)
        foreach (var p in inlineParts) merged[p.Name] = p;

        return [.. merged.Values];
    }

    // ── HTML post-processing ────────────────────────────────────────────────────

    private static readonly Regex RxLink    = new(@"<a\s[^>]*href=""(https?://[^""]+)""[^>]*>", RegexOptions.IgnoreCase);

    private static string PostProcessHtml(string html)
    {
        // External links: add target/_blank + rel
        html = RxLink.Replace(html, m =>
        {
            var tag = m.Value;
            if (tag.Contains("target=")) return tag;
            return tag.Replace(">", " target=\"_blank\" rel=\"noopener noreferrer\">");
        });

        return html;
    }

    private static string HtmlEncode(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}

// Extension to allow FrontmatterParser to expose raw fields for element_name override
static class FrontmatterExtensions
{
    public static Dictionary<string, string> RawFields(this Frontmatter _)
        => []; // placeholder — raw fields used only for element_name; implement if needed
}
