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

                var cleanSlug = p.Slug.Equals("home", StringComparison.OrdinalIgnoreCase)
                    ? ""
                    : p.Slug.EndsWith("/home", StringComparison.OrdinalIgnoreCase)
                        ? p.Slug[..^5]
                        : p.Slug;
                var loc = string.IsNullOrEmpty(cleanSlug)
                    ? $"https://{hostName}/"
                    : $"https://{hostName}/{cleanSlug}";

                return new XElement(Ns + "url",
                    new XElement(Ns + "loc",        loc),
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

    /// <summary>
    /// Updates an existing sitemap.xml in-place: replaces or inserts entries for the
    /// supplied pages only; all other entries are preserved unchanged.
    /// Re-sorts all entries by &lt;loc&gt; after updating and saves back to the same path.
    /// </summary>
    public static void Update(
        IReadOnlyList<PageInfo> updatedPages,
        string sitemapPath,
        string hostName)
    {
        var doc    = XDocument.Load(sitemapPath);
        var urlset = doc.Root!;

        // Index existing entries by <loc> while tolerating and cleaning duplicates.
        // Older sitemap files can contain duplicate URLs; keep the first and drop extras.
        var existingByLoc = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var el in urlset.Elements(Ns + "url").ToList())
        {
            var locValue = el.Element(Ns + "loc")?.Value ?? "";
            if (existingByLoc.ContainsKey(locValue))
            {
                el.Remove();
                continue;
            }

            existingByLoc[locValue] = el;
        }

        foreach (var p in updatedPages)
        {
            var lastMod = p.Frontmatter.Date
                ?? p.LastModified.ToString("yyyy-MM-dd");

            var cleanSlug = p.Slug.Equals("home", StringComparison.OrdinalIgnoreCase)
                ? ""
                : p.Slug.EndsWith("/home", StringComparison.OrdinalIgnoreCase)
                    ? p.Slug[..^5]
                    : p.Slug;
            var loc = string.IsNullOrEmpty(cleanSlug)
                ? $"https://{hostName}/"
                : $"https://{hostName}/{cleanSlug}";

            var newElement = new XElement(Ns + "url",
                new XElement(Ns + "loc",        loc),
                new XElement(Ns + "lastmod",    lastMod),
                new XElement(Ns + "changefreq", p.Frontmatter.ChangeFreq),
                new XElement(Ns + "priority",   p.Frontmatter.Priority
                    .ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture)));

            if (existingByLoc.TryGetValue(loc, out var existing))
            {
                existing.ReplaceWith(newElement);
                existingByLoc[loc] = newElement;
            }
            else
            {
                urlset.Add(newElement);
                existingByLoc[loc] = newElement;
            }
        }

        // Re-sort all entries by <loc>
        var sorted = urlset
            .Elements(Ns + "url")
            .OrderBy(el => el.Element(Ns + "loc")?.Value ?? "")
            .ToList();
        urlset.RemoveNodes();
        foreach (var el in sorted)
            urlset.Add(el);

        doc.Save(sitemapPath);
    }
}
