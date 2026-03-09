using AngleSharp.Html.Parser;

namespace Scraibe.Publisher;

/// <summary>
/// Rewrites relative URLs in generated HTML to root-relative form, using the
/// source markdown file directory (relative to /content) as resolution base.
/// </summary>
static class UrlRewriter
{
    private static readonly HtmlParser HtmlParser = new();

    public static string Rewrite(string html, string contentRelativeDir)
    {
        var baseDir = ToBaseDir(contentRelativeDir);
        var doc = HtmlParser.ParseDocument($"<!doctype html><html><body>{html}</body></html>");

        RewriteAttr(doc, "img", "src", baseDir, convertMarkdownLinks: false);
        RewriteAttr(doc, "a", "href", baseDir, convertMarkdownLinks: true);
        RewriteAttr(doc, "source", "src", baseDir, convertMarkdownLinks: false);
        RewriteSrcSet(doc, baseDir);
        RewriteAttr(doc, "video", "src", baseDir, convertMarkdownLinks: false);
        RewriteAttr(doc, "audio", "src", baseDir, convertMarkdownLinks: false);
        RewriteAttr(doc, "script", "src", baseDir, convertMarkdownLinks: false);
        RewriteAttr(doc, "link", "href", baseDir, convertMarkdownLinks: false);
        ApplyAssetLinkTargets(doc);

        return doc.Body?.InnerHtml ?? html;
    }

    private static void ApplyAssetLinkTargets(AngleSharp.Dom.IDocument doc)
    {
        foreach (var anchor in doc.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            if (!IsAssetLink(href))
            {
                continue;
            }

            anchor.SetAttribute("target", "_blank");
        }
    }

    private static bool IsAssetLink(string href)
    {
        var (pathPart, _) = SplitPathAndSuffix(href);
        var extension = Path.GetExtension(pathPart);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return !extension.Equals(".html", StringComparison.OrdinalIgnoreCase);
    }

    private static void RewriteAttr(AngleSharp.Dom.IDocument doc, string selector, string attributeName, string baseDir, bool convertMarkdownLinks)
    {
        foreach (var element in doc.QuerySelectorAll(selector))
        {
            var current = element.GetAttribute(attributeName);
            if (string.IsNullOrWhiteSpace(current))
            {
                continue;
            }

            var rewritten = RewriteUrl(current, baseDir, convertMarkdownLinks);
            if (!string.Equals(rewritten, current, StringComparison.Ordinal))
            {
                element.SetAttribute(attributeName, rewritten);
            }
        }
    }

    private static void RewriteSrcSet(AngleSharp.Dom.IDocument doc, string baseDir)
    {
        foreach (var element in doc.QuerySelectorAll("source[srcset]"))
        {
            var srcSet = element.GetAttribute("srcset");
            if (string.IsNullOrWhiteSpace(srcSet))
            {
                continue;
            }

            var rewritten = string.Join(", ",
                srcSet
                    .Split(',')
                    .Select(part => part.Trim())
                    .Where(part => part.Length > 0)
                    .Select(candidate =>
                    {
                        var splitIndex = candidate.IndexOfAny([' ', '\t']);
                        if (splitIndex < 0)
                        {
                            return RewriteUrl(candidate, baseDir, convertMarkdownLinks: false);
                        }

                        var url = candidate[..splitIndex];
                        var descriptor = candidate[splitIndex..].TrimStart();
                        var rewrittenUrl = RewriteUrl(url, baseDir, convertMarkdownLinks: false);
                        return $"{rewrittenUrl} {descriptor}";
                    }));

            if (!string.Equals(rewritten, srcSet, StringComparison.Ordinal))
            {
                element.SetAttribute("srcset", rewritten);
            }
        }
    }

    private static string RewriteUrl(string url, string baseDir, bool convertMarkdownLinks)
    {
        if (!IsRelativeUrl(url))
        {
            return url;
        }

        var (pathPart, suffix) = SplitPathAndSuffix(url);
        var rootedPath = ResolveRelativePath(baseDir, pathPart);

        if (convertMarkdownLinks && rootedPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            rootedPath = rootedPath[..^3];
            if (rootedPath.EndsWith("/home", StringComparison.OrdinalIgnoreCase))
            {
                rootedPath = rootedPath[..^5];
            }

            if (rootedPath.Length == 0)
            {
                rootedPath = "/";
            }
        }

        return rootedPath + suffix;
    }

    private static bool IsRelativeUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.StartsWith('/') ||
            value.StartsWith('#') ||
            value.StartsWith("//", StringComparison.Ordinal) ||
            value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var schemeSeparator = value.IndexOf(':');
        if (schemeSeparator > 0)
        {
            var scheme = value[..schemeSeparator];
            if (scheme.All(ch => char.IsLetterOrDigit(ch) || ch is '+' or '-' or '.'))
            {
                return false;
            }
        }

        return true;
    }

    private static (string PathPart, string Suffix) SplitPathAndSuffix(string url)
    {
        var queryIndex = url.IndexOf('?');
        var fragmentIndex = url.IndexOf('#');

        var cut = -1;
        if (queryIndex >= 0 && fragmentIndex >= 0)
        {
            cut = Math.Min(queryIndex, fragmentIndex);
        }
        else if (queryIndex >= 0)
        {
            cut = queryIndex;
        }
        else if (fragmentIndex >= 0)
        {
            cut = fragmentIndex;
        }

        if (cut < 0)
        {
            return (url, string.Empty);
        }

        return (url[..cut], url[cut..]);
    }

    private static string ResolveRelativePath(string baseDir, string relativePath)
    {
        var combined = baseDir + relativePath;
        var segments = combined.Split('/', StringSplitOptions.None);
        var stack = new List<string>();

        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment) || segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (stack.Count > 0)
                {
                    stack.RemoveAt(stack.Count - 1);
                }

                continue;
            }

            stack.Add(segment);
        }

        return "/" + string.Join('/', stack);
    }

    private static string ToBaseDir(string contentRelativeDir)
    {
        if (string.IsNullOrWhiteSpace(contentRelativeDir))
        {
            return "/";
        }

        var normalized = contentRelativeDir.Replace('\\', '/').Trim('/');
        return "/" + normalized + "/";
    }
}
