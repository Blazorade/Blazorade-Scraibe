using System.Text;

namespace Scraibe.Publisher;

/// <summary>
/// Generates the Bootstrap navbar inner HTML for the auto-nav part.
/// Produces the content that goes INSIDE &lt;nav x-part="nav"&gt; — not the nav element itself.
/// </summary>
static class NavGenerator
{
    /// <summary>
    /// Builds the Bootstrap 5 navbar inner HTML from the full set of published pages.
    /// </summary>
    public static string Generate(IReadOnlyList<PageInfo> allPages, string displayName)
    {
        // Collect root-level flat pages (depth 1, excluding home.md itself)
        var rootPages = allPages
            .Where(p => !p.RelativePath.Contains('/') &&
                        !IsHome(p.RelativePath))
            .OrderBy(p => p.Frontmatter.Title)
            .ToList();

        // Collect top-level subdirectories and their pages
        var subdirs = allPages
            .Where(p => p.RelativePath.Contains('/'))
            .GroupBy(p => p.RelativePath.Split('/')[0])
            .OrderBy(g => g.Key)
            .ToList();

        var sb  = new StringBuilder();
        var uid = "navbar-collapse-main";

        sb.AppendLine("<div class=\"container-fluid\">");

        // Brand
        sb.AppendLine($"  <a class=\"navbar-brand\" href=\"/\">{HtmlEncode(displayName)}</a>");

        // Toggler
        sb.AppendLine($"  <button class=\"navbar-toggler\" type=\"button\"");
        sb.AppendLine($"    data-bs-toggle=\"collapse\" data-bs-target=\"#{uid}\"");
        sb.AppendLine($"    aria-controls=\"{uid}\" aria-expanded=\"false\" aria-label=\"Toggle navigation\">");
        sb.AppendLine("    <span class=\"navbar-toggler-icon\"></span>");
        sb.AppendLine("  </button>");

        // Collapsible nav
        sb.AppendLine($"  <div class=\"collapse navbar-collapse\" id=\"{uid}\">");
        sb.AppendLine("    <ul class=\"navbar-nav me-auto mb-2 mb-lg-0\">");

        // Flat root pages
        foreach (var page in rootPages)
        {
            var href  = CleanUrl(page.Slug);
            var label = HtmlEncode(page.Frontmatter.Title);
            sb.AppendLine($"      <li class=\"nav-item\">");
            sb.AppendLine($"        <a class=\"nav-link\" href=\"{href}\">{label}</a>");
            sb.AppendLine("      </li>");
        }

        // Subdir dropdowns
        foreach (var group in subdirs)
        {
            var dirKey   = group.Key;
            var homePage = group.FirstOrDefault(p => IsHome(Path.GetFileName(p.RelativePath)));
            var label    = homePage != null
                ? HtmlEncode(homePage.Frontmatter.Title)
                : HtmlEncode(Capitalise(dirKey));
            var itemId  = $"dropdown-{dirKey}";
            var homeUrl = homePage != null ? CleanUrl(homePage.Slug) : $"/{dirKey}";

            sb.AppendLine($"      <li class=\"nav-item dropdown\">");
            sb.AppendLine($"        <a class=\"nav-link dropdown-toggle\" href=\"{homeUrl}\" role=\"button\"");
            sb.AppendLine($"          data-bs-toggle=\"dropdown\" aria-expanded=\"false\">{label}</a>");
            sb.AppendLine($"        <ul class=\"dropdown-menu\" aria-labelledby=\"{itemId}\">");

            if (homePage != null)
            {
                sb.AppendLine($"          <li><a class=\"dropdown-item\" href=\"{CleanUrl(homePage.Slug)}\">Overview</a></li>");
                sb.AppendLine("          <li><hr class=\"dropdown-divider\" /></li>");
            }

            foreach (var page in group.Where(p => p != homePage).OrderBy(p => p.Frontmatter.Title))
            {
                sb.AppendLine($"          <li><a class=\"dropdown-item\" href=\"{CleanUrl(page.Slug)}\">{HtmlEncode(page.Frontmatter.Title)}</a></li>");
            }

            sb.AppendLine("        </ul>");
            sb.AppendLine("      </li>");
        }

        sb.AppendLine("    </ul>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");

        return sb.ToString().TrimEnd();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static bool IsHome(string relativeFileOrSegment)
        => Path.GetFileNameWithoutExtension(relativeFileOrSegment)
               .Equals("home", StringComparison.OrdinalIgnoreCase);

    /// <summary>Converts a page slug into a clean URL (no .html, leading slash).</summary>
    private static string CleanUrl(string slug)
    {
        // home.md at any level maps to its parent directory clean URL
        if (slug.Equals("home", StringComparison.OrdinalIgnoreCase))
            return "/";
        // products/home → /products
        if (slug.EndsWith("/home", StringComparison.OrdinalIgnoreCase))
            return "/" + slug[..^5];
        return "/" + slug;
    }

    private static string Capitalise(string s)
        => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static string HtmlEncode(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
