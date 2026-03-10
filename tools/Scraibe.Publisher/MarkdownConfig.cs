using Markdig;
using Markdig.Parsers;

namespace Scraibe.Publisher;

/// <summary>
/// Centralized markdown configuration for the publish pipeline.
///
/// We intentionally disable indented code blocks so authors must use fenced
/// code blocks (``` or ~~~). This avoids accidental code blocks caused by
/// readability indentation inside wrapping shortcodes.
/// </summary>
static class MarkdownConfig
{
    public static readonly MarkdownPipeline Pipeline = CreatePipeline();

    private static MarkdownPipeline CreatePipeline()
    {
        var builder = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions();

        var indentedCodeParsers = builder.BlockParsers
            .Where(p => p.GetType().Name.Equals(nameof(IndentedCodeBlockParser), StringComparison.Ordinal))
            .ToList();

        foreach (var parser in indentedCodeParsers)
            builder.BlockParsers.Remove(parser);

        return builder.Build();
    }
}
