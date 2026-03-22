namespace Scraibe.Publisher;

/// <summary>
/// Splits a Markdown file into a <see cref="Frontmatter"/> record and the raw Markdown body.
/// Handles YAML frontmatter delimited by --- lines. All fields are optional.
/// </summary>
static class FrontmatterParser
{
    public static (Frontmatter frontmatter, string body) Parse(string fileContent, string fileName)
    {
        var lines = fileContent.ReplaceLineEndings("\n").Split('\n');
        var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int bodyStart = 0;

        if (lines.Length > 0 && lines[0].Trim() == "---")
        {
            int end = -1;
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "---") { end = i; break; }
            }
            if (end > 0)
            {
                for (int i = 1; i < end; i++)
                {
                    var colonIdx = lines[i].IndexOf(':');
                    if (colonIdx <= 0) continue;
                    var key = lines[i][..colonIdx].Trim();
                    var val = lines[i][(colonIdx + 1)..].Trim();
                    // Strip inline YAML quotes
                    if (val.Length >= 2 && val[0] == '"' && val[^1] == '"')
                        val = val[1..^1];
                    else if (val.Length >= 2 && val[0] == '\'' && val[^1] == '\'')
                        val = val[1..^1];
                    // Accumulate multi-line block scalars (ai_instructions etc.) — just skip
                    if (val == "|" || val == ">") continue;
                    raw[key] = val;
                }
                bodyStart = end + 1;
            }
        }

        var body = string.Join('\n', lines[bodyStart..]);

        // Derive title fallback from first # heading or filename
        string title = raw.GetValueOrDefault("title", "");
        if (string.IsNullOrWhiteSpace(title))
        {
            foreach (var line in lines[bodyStart..])
            {
                if (line.StartsWith("# ")) { title = line[2..].Trim(); break; }
            }
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            title = Path.GetFileNameWithoutExtension(fileName);
            title = char.ToUpperInvariant(title[0]) + title[1..];
        }

        // Layout is optional. When absent, publish orchestration resolves default
        // from effective folder configuration (scraibe.layout.default).
        string? layout = null;
        if (raw.TryGetValue("layout", out var rawLayout) && !string.IsNullOrWhiteSpace(rawLayout))
            layout = ToPascalCase(rawLayout);

        double priority = 0.8;
        if (raw.TryGetValue("priority", out var priStr))
            double.TryParse(priStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out priority);

        var schemaType = raw.GetValueOrDefault("schema_type", "WebPage");
        if (string.IsNullOrWhiteSpace(schemaType))
            schemaType = "WebPage";

        return (
            new Frontmatter(
                Title:       title,
                Description: raw.GetValueOrDefault("description"),
                Slug:        raw.GetValueOrDefault("slug"),
                SchemaType:  schemaType,
                Keywords:    raw.GetValueOrDefault("keywords"),
                Author:      raw.GetValueOrDefault("author"),
                Date:        raw.GetValueOrDefault("date"),
                Layout:      layout,
                ChangeFreq:  raw.GetValueOrDefault("changefreq", "monthly"),
                Priority:    priority,
                RawFields:   new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase)
            ),
            body
        );
    }

    /// <summary>Normalises a kebab-case or lowercase layout name to PascalCase.</summary>
    private static string ToPascalCase(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "Default";
        var parts = s.Split('-', '_', ' ');
        return string.Concat(parts.Select(p =>
            p.Length == 0 ? "" : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }
}
