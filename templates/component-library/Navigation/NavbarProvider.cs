using Scraibe.Abstractions.Annotation;
using Scraibe.Abstractions.Configuration;
using Scraibe.Abstractions.Navigation;
using System.Text;

namespace {{ComponentLibraryName}}.Navigation;

[ProviderName("navbar")]
/// <summary>
/// Renders the default Bootstrap navbar markup from a normalized navigation model.
/// </summary>
public sealed class NavbarProvider : INavigationMarkupProvider
{
    /// <summary>
    /// Creates the navbar HTML consumed by the publish pipeline when the <c>navbar</c> provider is selected.
    /// </summary>
    /// <param name="model">The normalized navigation structure for the current page context.</param>
    /// <param name="effectiveConfiguration">Effective folder configuration used for provider-level decisions.</param>
    /// <returns>Provider markup with a single root <c>nav</c> element.</returns>
    public string CreateMarkup(NavigationModel model, IReadOnlyDictionary<string, object?> effectiveConfiguration)
    {
        var sb = new StringBuilder();
        var uid = "navbar-collapse-main";
        var brandTitle = ResolveBrandTitle(effectiveConfiguration);

        sb.AppendLine("<nav class=\"navbar\">");
        sb.AppendLine("  <div class=\"container-fluid\">");
        sb.AppendLine($"    <a class=\"navbar-brand\" href=\"/\">{HtmlEncode(brandTitle)}</a>");

        sb.AppendLine("    <button class=\"navbar-toggler\" type=\"button\"");
        sb.AppendLine($"      data-bs-toggle=\"collapse\" data-bs-target=\"#{uid}\"");
        sb.AppendLine($"      aria-controls=\"{uid}\" aria-expanded=\"false\" aria-label=\"Toggle navigation\">");
        sb.AppendLine("      <span class=\"navbar-toggler-icon\"></span>");
        sb.AppendLine("    </button>");

        sb.AppendLine($"    <div class=\"collapse navbar-collapse\" id=\"{uid}\">");
        sb.AppendLine("      <ul class=\"navbar-nav me-auto mb-2 mb-lg-0\">");

        var up = model.Ancestors.FirstOrDefault();
        if (up is not null)
        {
            sb.AppendLine("        <li class=\"nav-item\">");
            sb.AppendLine($"          <a class=\"nav-link\" href=\"{HtmlEncode(up.Url)}\" aria-label=\"Up\" title=\"Up\"><span aria-hidden=\"true\" style=\"font-family: Wingdings, Webdings, sans-serif;\">&#x2191;</span></a>");
            sb.AppendLine("        </li>");

            sb.AppendLine("        <li class=\"nav-item\" aria-hidden=\"true\">");
            sb.AppendLine("          <span class=\"nav-link px-4\">|</span>");
            sb.AppendLine("        </li>");
        }

        foreach (var item in model.Items)
        {
            if (item.Children.Count > 0)
            {
                sb.AppendLine("        <li class=\"nav-item dropdown\">");
                sb.AppendLine($"          <a class=\"nav-link dropdown-toggle\" href=\"#\" role=\"button\" data-bs-toggle=\"dropdown\" aria-expanded=\"false\">{HtmlEncode(item.Title)}</a>");
                sb.AppendLine("          <ul class=\"dropdown-menu\">");

                if (!string.IsNullOrWhiteSpace(item.Url))
                {
                    sb.AppendLine($"            <li><a class=\"dropdown-item\" href=\"{HtmlEncode(item.Url)}\">{HtmlEncode(item.Title)}</a></li>");
                }

                RenderDropdownChildren(sb, item.Children, level: 1);

                sb.AppendLine("          </ul>");
                sb.AppendLine("        </li>");
                continue;
            }

            sb.AppendLine("        <li class=\"nav-item\">");
            sb.AppendLine($"          <a class=\"nav-link\" href=\"{HtmlEncode(item.Url)}\">{HtmlEncode(item.Title)}</a>");
            sb.AppendLine("        </li>");
        }

        sb.AppendLine("      </ul>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</nav>");

        return sb.ToString().TrimEnd();
    }

    private static void RenderDropdownChildren(StringBuilder sb, IReadOnlyList<NavigationItem> children, int level)
    {
        foreach (var child in children)
        {
            if (child.Children.Count == 0 && string.IsNullOrWhiteSpace(child.Url))
            {
                // Omit non-clickable empty leaf items.
                continue;
            }

            if (child.Children.Count > 0)
            {
                sb.AppendLine("            <li class=\"dropdown-submenu\">");

                sb.AppendLine($"              <button type=\"button\" class=\"dropdown-item dropdown-toggle\" aria-expanded=\"false\">{HtmlEncode(child.Title)}</button>");

                sb.AppendLine("              <ul class=\"dropdown-menu\">");

                if (!string.IsNullOrWhiteSpace(child.Url))
                {
                    sb.AppendLine($"                <li><a class=\"dropdown-item\" href=\"{HtmlEncode(child.Url)}\">{HtmlEncode(child.Title)}</a></li>");
                }

                RenderDropdownChildren(sb, child.Children, level + 1);
                sb.AppendLine("              </ul>");
                sb.AppendLine("            </li>");
            }
            else
            {
                sb.AppendLine($"            <li><a class=\"dropdown-item\" href=\"{HtmlEncode(child.Url)}\">{HtmlEncode(child.Title)}</a></li>");
            }
        }
    }

    private static string ResolveBrandTitle(IReadOnlyDictionary<string, object?> effectiveConfiguration)
    {
        if (effectiveConfiguration.TryGetValue(ConfigKeys.ScraibeSiteDisplayName, out var displayName)
            && displayName is string text
            && !string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return "Home";
    }

    private static string HtmlEncode(string value)
        => value.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}
