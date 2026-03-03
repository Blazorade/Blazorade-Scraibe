namespace Scraibe.Publisher;

/// <summary>All configuration resolved from CLI args and blazorade.config.md.</summary>
record PublishOptions(
    string ContentPath,           // absolute path to /content
    string OutputPath,            // absolute path to wwwroot
    string HostName,              // e.g. blazorade.com
    string DisplayName,           // e.g. Blazorade
    string TemplatePath,          // absolute path to page-template.html
    string AssemblyPath,          // absolute path to compiled component library DLL
    string ComponentNamespace,    // e.g. BlazoradeCom.Components.ShortCodes
    string LayoutsPath,           // absolute path to Layouts folder in component library
    List<string> ExcludedPaths,   // paths relative to /content, read from blazorade.config.md
    string BlazorScript           // e.g. _framework/blazor.webassembly#[.abc123].js
);

/// <summary>Resolved metadata for one publishable page.</summary>
record PageInfo(
    string SourcePath,      // absolute path to the .md source file
    string RelativePath,    // relative to /content, e.g. "products/widget.md"
    string OutputPath,      // absolute path to wwwroot target, e.g. ".../wwwroot/products/widget.html"
    string Slug,            // e.g. "products/widget"   (no leading slash, no .html)
    string CanonicalUrl,    // e.g. https://blazorade.com/products/widget.html
    Frontmatter Frontmatter,
    DateTime LastModified
);

/// <summary>Parsed YAML frontmatter for one page.</summary>
record Frontmatter(
    string Title,
    string? Description,
    string? Slug,          // override for the output filename
    string? Keywords,
    string? Author,
    string? Date,          // ISO date string if explicitly set in frontmatter
    string Layout,         // PascalCase layout name, defaults to "Default"
    string ChangeFreq,     // sitemap changefreq, defaults to "monthly"
    double Priority        // sitemap priority, defaults to 0.8
);

/// <summary>A named HTML fragment to be injected into an x-part slot in the layout.</summary>
record PartInfo(
    string Name,          // e.g. "nav", "footer", "right-panel"
    string ElementName,   // e.g. "nav", "footer", "aside"
    string InnerHtml      // inner HTML content (NOT wrapped in the element tag — PagePublisher wraps it)
);

/// <summary>Outcome of publishing one page.</summary>
record PageResult(
    PageInfo Page,
    bool Success,
    string? Error = null
);

/// <summary>Maps "part name" → HTML element name per the instructions.</summary>
static class ElementNames
{
    public static string For(string partName) => partName.ToLowerInvariant() switch
    {
        "header" => "header",
        "nav"    => "nav",
        "main"   => "main",
        "footer" => "footer",
        _        => "aside"
    };
}
