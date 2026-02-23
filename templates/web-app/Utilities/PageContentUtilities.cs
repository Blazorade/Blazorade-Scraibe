namespace {{WebAppName}}.Utilities;

public static class PageContentUtilities
{
    public static string ExtractTitle(string html)
    {
        var start = html.IndexOf("<title>", StringComparison.OrdinalIgnoreCase);
        var end = html.IndexOf("</title>", StringComparison.OrdinalIgnoreCase);
        if (start >= 0 && end > start)
        {
            return html[(start + 7)..end].Trim();
        }

        return string.Empty;
    }
}
