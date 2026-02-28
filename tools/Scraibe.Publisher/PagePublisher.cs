using System.Text;
using System.Text.RegularExpressions;
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
            bodyHtml = PostProcessHtml(bodyHtml);

            // 7. Merge parts: [Part] shortcodes → scoped _name.md files → (nav always present)
            var parts = MergeParts(inlineParts, scopedParts, navHtml);

            // 8. Apply page template
            var html = ApplyTemplate(page, bodyHtml, parts, opts);

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

    private static string ApplyTemplate(PageInfo page, string bodyHtml,
        List<PartInfo> parts, PublishOptions opts)
    {
        var template  = File.ReadAllText(opts.TemplatePath);
        var fm        = page.Frontmatter;
        var lastMod   = fm.Date ?? page.LastModified.ToString("yyyy-MM-dd");

        // Serialize parts_html: all parts except "main" (main is handled by the template)
        var partsHtml = new StringBuilder();
        foreach (var part in parts.Where(p => p.Name != "main"))
        {
            partsHtml.AppendLine(
                $"  <{part.ElementName} hidden x-part=\"{part.Name}\">{part.InnerHtml}" +
                $"</{part.ElementName}>");
        }

        var result = template;

        // Mandatory tokens
        result = result.Replace("{title}",       HtmlEncode(fm.Title));
        result = result.Replace("{description}", HtmlEncode(fm.Description ?? ""));
        result = result.Replace("{slug}",        page.Slug);
        result = result.Replace("{HostName}",    opts.HostName);
        result = result.Replace("{layout}",      fm.Layout);
        result = result.Replace("{date}",        lastMod);
        result = result.Replace("{body_html}",   bodyHtml);
        result = result.Replace("{parts_html}",  partsHtml.ToString().TrimEnd());

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

                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                var innerHtml = Markdig.Markdown.ToHtml(body, pipeline).Trim();
                result[partName] = new PartInfo(partName, elemName, innerHtml);
            }
        }

        return result.Values.ToList();
    }

    private static List<PartInfo> MergeParts(
        List<PartInfo> inlineParts,
        List<PartInfo> scopedParts,
        string navHtml)
    {
        // Priority: [Part] shortcode > _name.md > auto-nav
        var merged = new Dictionary<string, PartInfo>(StringComparer.OrdinalIgnoreCase);

        // Auto-nav first (lowest priority)
        merged["nav"] = new PartInfo("nav", "nav", navHtml);

        // Scoped _name.md files
        foreach (var p in scopedParts) merged[p.Name] = p;

        // Inline [Part] shortcodes (highest)
        foreach (var p in inlineParts) merged[p.Name] = p;

        return [.. merged.Values];
    }

    // ── HTML post-processing ────────────────────────────────────────────────────

    private static readonly Regex RxLink    = new(@"<a\s[^>]*href=""(https?://[^""]+)""[^>]*>", RegexOptions.IgnoreCase);
    private static readonly Regex RxMdLink  = new(@"href=""([^""]+\.md)(#[^""]*)?""",            RegexOptions.IgnoreCase);

    private static string PostProcessHtml(string html)
    {
        // Rewrite .md links → .html (preserve #fragments)
        html = RxMdLink.Replace(html, m =>
        {
            var path = m.Groups[1].Value.Replace(".md", ".html");
            var frag = m.Groups[2].Value;
            return $"href=\"{path}{frag}\"";
        });

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
