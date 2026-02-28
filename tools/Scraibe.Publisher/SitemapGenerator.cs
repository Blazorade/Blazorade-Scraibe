using System.Xml.Linq;

namespace Scraibe.Publisher;

/// <summary>
/// Generates sitemap.xml for the set of published pages using System.Xml.Linq.
/// Output is always well-formed XML — no template file required.
/// </summary>
static class SitemapGenerator
{
    static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public static void Generate(
        IReadOnlyList<PageInfo> pages,
        string outputPath,
        string hostName)
    {
        var urlElements = pages
            .OrderBy(p => p.Slug)
            .Select(p =>
            {
                var lastMod = p.Frontmatter.Date
                    ?? p.LastModified.ToString("yyyy-MM-dd");

                return new XElement(Ns + "url",
                    new XElement(Ns + "loc",        $"https://{hostName}/{p.Slug}.html"),
                    new XElement(Ns + "lastmod",    lastMod),
                    new XElement(Ns + "changefreq", p.Frontmatter.ChangeFreq),
                    new XElement(Ns + "priority",   p.Frontmatter.Priority
                        .ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture)));
            });

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Ns + "urlset", urlElements));

        doc.Save(outputPath);
    }
}
