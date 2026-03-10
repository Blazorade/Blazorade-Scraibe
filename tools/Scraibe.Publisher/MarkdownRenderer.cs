using Markdig;

namespace Scraibe.Publisher;

static class MarkdownRenderer
{
    public static string ToHtml(string markdown)
        => Markdig.Markdown.ToHtml(markdown, MarkdownConfig.Pipeline);
}
