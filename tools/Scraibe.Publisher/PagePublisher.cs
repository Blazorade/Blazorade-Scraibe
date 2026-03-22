using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Scraibe.Abstractions.Configuration;
using Scraibe.Abstractions.Content;
using Scraibe.ContentComposition.Html;

namespace Scraibe.Publisher;

/// <summary>
/// Orchestrates the publish pipeline for a single page:
///   read markdown → parse frontmatter → resolve parts → process shortcodes
///   → convert to HTML → apply template → write output file.
/// </summary>
static class PagePublisher
{
    /// <summary>
    /// Publishes one page. Returns a <see cref="PageResult"/> — never throws.
    /// The caller provides pre-built nav HTML so every page gets the same navbar.
    /// </summary>
    public static PageResult Publish(
        PageInfo page,
        ComponentRegistry registry,
        NavigationMarkupProviderFactory navigationProviders,
        SlotContentProviderFactory slotContentProviders,
        PublishOptions opts,
        IReadOnlyList<PageInfo> allPages)
    {
        try
        {
            var layoutName = page.Frontmatter.Layout;
            if (string.IsNullOrWhiteSpace(layoutName))
            {
                return new PageResult(page, false,
                    "Layout could not be resolved for page. Set frontmatter layout or define scraibe.layout.default in effective folder configuration.");
            }

            // 1. Read source fresh from disk
            var raw = File.ReadAllText(page.SourcePath);

            // 2. Frontmatter (already parsed into page.Frontmatter, but body is re-extracted here)
            var (_, body) = FrontmatterParser.Parse(raw, page.SourcePath);

            // 3. Verify layout exists
            var layoutFile = Path.Combine(opts.LayoutsPath, layoutName + ".html");
            if (!File.Exists(layoutFile))
            {
                // Case-insensitive fallback
                var match = Directory.GetFiles(opts.LayoutsPath, "*.html")
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                        .Equals(layoutName, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                    return new PageResult(page, false,
                        $"Layout '{layoutName}' not found in '{opts.LayoutsPath}'.");
            }

            // 4. Resolve _name.md scoped parts (walk up from page's dir to /content)
            var scopedParts = ResolveScopedParts(page, opts, registry, allPages);

            // 5. Process shortcodes (returns processed markdown + [Slot] entries)
            var scResult = ShortcodeProcessor.Process(body, registry, page.SourcePath, opts);
            var inlineParts = scResult.Parts;
            var contentRelativeDir = Path.GetDirectoryName(page.RelativePath)?.Replace('\\', '/') ?? "";

            // Inline [Slot] shortcode content must flow through the same URL rewrite
            // and post-process path as page body/scoped parts.
            inlineParts = inlineParts
                .Select(p => new PartInfo(
                    p.Name,
                    p.ElementName,
                    PostProcessHtml(UrlRewriter.Rewrite(p.InnerHtml, contentRelativeDir))))
                .ToList();

            // 5b. Resolve auto-nav for this page only when no scoped/inline nav override exists.
            var hasNavOverride = scopedParts.Any(p => p.Name.Equals("nav", StringComparison.OrdinalIgnoreCase))
                || inlineParts.Any(p => p.Name.Equals("nav", StringComparison.OrdinalIgnoreCase));

            var navPart = ResolveNavigationPart(
                page,
                navigationProviders,
                opts,
                allPages,
                hasNavOverride);

            // 6. Convert processed markdown to HTML
            var bodyHtml = MarkdownRenderer.ToHtml(scResult.ProcessedMarkdown);

            // Inject wrapping-shortcode inner HTML now that Markdig has run.
            // This must happen BEFORE PostProcessHtml so that .md links inside shortcode
            // inner content are also rewritten to .html.
            // (Inner HTML was deferred out of ProcessedMarkdown to prevent Markdig's
            // condition-6 HTML blocks from terminating at blank lines inside the content.)
            foreach (var (placeholder, innerHtml) in scResult.ShortcodeInners)
                bodyHtml = bodyHtml.Replace($"<!--{placeholder}-->", innerHtml);

            bodyHtml = UrlRewriter.Rewrite(bodyHtml, contentRelativeDir);

            bodyHtml = PostProcessHtml(bodyHtml);

            // 7. Merge parts: [Slot] shortcodes → scoped _name.md files → page body → nav
            var parts = MergeParts(inlineParts, scopedParts, navPart, bodyHtml);

            // 8. Apply page template
            var html = ApplyTemplate(page, parts, slotContentProviders, opts);

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

    private static string ApplyTemplate(PageInfo page, List<PartInfo> parts, SlotContentProviderFactory slotContentProviders, PublishOptions opts)
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

        // Build layout_html by injecting part content into each x-slot slot.
        var layoutName = fm.Layout ?? throw new PublishException(
            "Layout could not be resolved for page. Set frontmatter layout or define scraibe.layout.default in effective folder configuration.");

        var layoutFile = Path.Combine(opts.LayoutsPath, layoutName + ".html");
        if (!File.Exists(layoutFile))
        {
            layoutFile = Directory.GetFiles(opts.LayoutsPath, "*.html")
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                    .Equals(layoutName, StringComparison.OrdinalIgnoreCase)) ?? layoutFile;
        }
        var layoutHtml    = SlotComposer.NormalizeSelfClosingSlotElements(File.ReadAllText(layoutFile));
        var layoutDoc     = HtmlParser.ParseDocument(layoutHtml);
        var partsByName   = parts.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var slot in layoutDoc.QuerySelectorAll("[x-slot]").ToList())
        {
            var partName = slot.GetAttribute("x-slot") ?? "";
            if (partsByName.TryGetValue(partName, out var part))
            {
                if (part.ReplaceElement)
                {
                    slot.OuterHtml = SlotComposer.BuildReplacementRootHtml(
                        slot,
                        part.InnerHtml,
                        $"page '{page.RelativePath}'");
                }
                else
                {
                    var providerName = slot.GetAttribute("x-provider")?.Trim();
                    if (string.IsNullOrWhiteSpace(providerName))
                        providerName = GetConfiguredDefaultSlotProvider(page.EffectiveFolderConfig);

                    if (string.IsNullOrWhiteSpace(providerName))
                    {
                        throw new PublishException(
                            $"Slot content provider could not be resolved for page '{page.RelativePath}', layout '{Path.GetFileName(layoutFile)}', slot '{partName}'. Add x-provider on the layout slot or set '{ConfigKeys.ScraibeContentSlotProviderDefault}' in effective .config.json.");
                    }

                    var provider = slotContentProviders.ResolveOrThrow(
                        providerName,
                        $"page '{page.RelativePath}', layout '{Path.GetFileName(layoutFile)}', slot '{partName}'");

                    var elementNameHint = slot.LocalName;
                    var placeholderAttributes = slot.Attributes.ToDictionary(a => a.Name, a => a.Value);

                    var providerHtml = provider.CreateSlotContent(
                        elementNameHint,
                        part.InnerHtml,
                        placeholderAttributes,
                        page.EffectiveFolderConfig);

                    slot.OuterHtml = SlotComposer.BuildReplacementRootHtml(
                        slot,
                        providerHtml,
                        $"page '{page.RelativePath}', slot '{partName}', slot provider '{providerName}'");
                }
            }
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
        result = result.Replace("{layout}",        layoutName);
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
                if (fm.RawFields.TryGetValue("element_name", out var elemOverride))
                    elemName = elemOverride;

                // Reusable parts should follow the same processing path as page main content.
                var scResult = ShortcodeProcessor.Process(body, registry, mdFile, opts);
                var partHtml = MarkdownRenderer.ToHtml(scResult.ProcessedMarkdown);

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
        PartInfo navPart,
        string bodyHtml)
    {
        // Priority (highest → lowest): [Slot] shortcode > _name.md > page body > auto-nav
        var merged = new Dictionary<string, PartInfo>(StringComparer.OrdinalIgnoreCase);

        // Auto-nav (lowest priority)
        merged["nav"]  = navPart;

        // Page body becomes the default "main" content
        merged["main"] = new PartInfo("main", "article", bodyHtml);

        // Scoped _name.md files overwrite (including _main.md if present)
        foreach (var p in scopedParts) merged[p.Name] = p;

        // Inline [Slot] shortcodes (highest priority)
        foreach (var p in inlineParts) merged[p.Name] = p;

        return [.. merged.Values];
    }

    private static PartInfo ResolveNavigationPart(
        PageInfo page,
        NavigationMarkupProviderFactory navigationProviders,
        PublishOptions opts,
        IReadOnlyList<PageInfo> allPages,
        bool hasNavOverride)
    {
        if (hasNavOverride)
            return new PartInfo("nav", "nav", "");

        var layoutName = page.Frontmatter.Layout ?? "Default";
        var layoutFile = Path.Combine(opts.LayoutsPath, layoutName + ".html");
        if (!File.Exists(layoutFile))
        {
            layoutFile = Directory.GetFiles(opts.LayoutsPath, "*.html")
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                    .Equals(layoutName, StringComparison.OrdinalIgnoreCase)) ?? layoutFile;
        }

        var layoutHtml = SlotComposer.NormalizeSelfClosingSlotElements(File.ReadAllText(layoutFile));
        var layoutDoc = HtmlParser.ParseDocument(layoutHtml);
        var navSlot = layoutDoc.QuerySelector("[x-slot='nav']");
        if (navSlot is null)
            return new PartInfo("nav", "nav", "");

        var fromLayout = navSlot.GetAttribute("x-provider")?.Trim();
        var fromConfig = GetConfiguredDefaultProvider(page.EffectiveFolderConfig);

        string providerName;
        if (!string.IsNullOrWhiteSpace(fromLayout))
        {
            providerName = fromLayout!;
        }
        else if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            providerName = fromConfig;
        }
        else
        {
            throw new PublishException(
                $"Navigation provider could not be resolved for page '{page.RelativePath}'. Add x-provider on the layout nav slot or set '{ConfigKeys.ScraibeNavigationProviderDefault}' in effective .config.json.");
        }

        var provider = navigationProviders.ResolveOrThrow(
            providerName,
            $"page '{page.RelativePath}', layout '{Path.GetFileName(layoutFile)}'");

        var model = NavGenerator.BuildModelForPage(
            page,
            allPages,
            opts.DisplayName,
            page.EffectiveFolderConfig);
        var navHtml = provider.CreateMarkup(model, page.EffectiveFolderConfig);

        return new PartInfo("nav", "nav", navHtml, ReplaceElement: true);
    }

    private static string? GetConfiguredDefaultProvider(Dictionary<string, object?> effectiveConfig)
    {
        if (effectiveConfig.TryGetValue(ConfigKeys.ScraibeNavigationProviderDefault, out var configured)
            && configured is string value
            && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return null;
    }

    private static string? GetConfiguredDefaultSlotProvider(Dictionary<string, object?> effectiveConfig)
    {
        if (effectiveConfig.TryGetValue(ConfigKeys.ScraibeContentSlotProviderDefault, out var configured)
            && configured is string value
            && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return null;
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
