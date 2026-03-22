using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Text.RegularExpressions;

namespace Scraibe.ContentComposition.Html;

/// <summary>
/// Provides shared HTML composition helpers for replacing layout slot elements with page part content.
/// </summary>
public static class SlotComposer
{
    private static readonly HtmlParser HtmlParser = new();
    private static readonly Regex SelfClosingSlotElementRegex = new(
        @"<(?<tag>[A-Za-z][A-Za-z0-9:_\-]*)(?<attrs>[^>]*\bx-slot\s*=\s*(?:""[^""]*""|'[^']*')[^>]*)/>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Converts self-closing slot elements (elements carrying <c>x-slot</c>) to explicit open/close form
    /// so HTML parsers preserve sibling structure for custom element names.
    /// </summary>
    /// <param name="html">Raw layout markup.</param>
    /// <returns>Layout markup where self-closing slot elements are expanded.</returns>
    public static string NormalizeSelfClosingSlotElements(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html ?? string.Empty;

        return SelfClosingSlotElementRegex.Replace(html, m =>
        {
            var tag = m.Groups["tag"].Value;
            var attrs = m.Groups["attrs"].Value;
            return $"<{tag}{attrs}></{tag}>";
        });
    }

    /// <summary>
    /// Replaces a slot element with the root element from source part HTML while preserving slot-level
    /// attributes and merging CSS classes with publish/runtime parity.
    /// </summary>
    /// <param name="slotElement">The slot element from the layout document (typically identified by <c>x-slot</c>).</param>
    /// <param name="sourcePartOuterHtml">The source part HTML containing exactly one root element.</param>
    /// <param name="errorContext">Optional context used in exception messages when source HTML is invalid.</param>
    /// <returns>The merged replacement root element as outer HTML.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="sourcePartOuterHtml"/> does not contain exactly one root element and
    /// <paramref name="errorContext"/> is provided.
    /// </exception>
    public static string BuildReplacementRootHtml(IElement slotElement, string sourcePartOuterHtml, string? errorContext = null)
    {
        var partDoc = HtmlParser.ParseDocument(sourcePartOuterHtml ?? string.Empty);
        var roots = partDoc.Body?.Children.ToList() ?? [];
        if (roots.Count != 1)
        {
            if (!string.IsNullOrWhiteSpace(errorContext))
            {
                throw new InvalidOperationException(
                    $"Replacement source must contain exactly one root element ({errorContext}).");
            }

            return sourcePartOuterHtml ?? string.Empty;
        }

        var root = roots[0];

        var slotClasses = TokenizeCssClasses(slotElement.GetAttribute("class"));
        var sourceClasses = TokenizeCssClasses(root.GetAttribute("class"));
        var mergedClasses = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var cls in slotClasses)
            if (seen.Add(cls)) mergedClasses.Add(cls);
        foreach (var cls in sourceClasses)
            if (seen.Add(cls)) mergedClasses.Add(cls);

        foreach (var attr in slotElement.Attributes)
        {
            if (attr.Name.Equals("class", StringComparison.OrdinalIgnoreCase))
                continue;

            root.SetAttribute(attr.Name, attr.Value);
        }

        if (mergedClasses.Count > 0)
            root.SetAttribute("class", string.Join(' ', mergedClasses));
        else
            root.RemoveAttribute("class");

        return root.OuterHtml ?? string.Empty;
    }

    private static List<string> TokenizeCssClasses(string? classValue)
    {
        if (string.IsNullOrWhiteSpace(classValue))
            return [];

        return classValue
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }
}
