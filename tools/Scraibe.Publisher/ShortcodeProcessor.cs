using System.Text;
using System.Text.RegularExpressions;
using Markdig;

namespace Scraibe.Publisher;

/// <summary>
/// Implements the stack-based shortcode parser described in publish.instructions.md.
/// Scans Markdown line-by-line, replaces known shortcodes with x-shortcode sentinel elements,
/// extracts [Part] blocks, and returns processed Markdown ready for Markdig conversion.
/// </summary>
static class ShortcodeProcessor
{
    // Regex patterns — applied in priority order per the spec
    private static readonly Regex RxSelfClose  = new(@"^\[([A-Za-z][A-Za-z0-9]*)([^\]]*)\s*/\]$",    RegexOptions.Compiled);
    private static readonly Regex RxInline     = new(@"^\[([A-Za-z][A-Za-z0-9]*)([^\]]*)\](.+)\[/\1\]$", RegexOptions.Compiled);
    private static readonly Regex RxOpen       = new(@"^\[([A-Za-z][A-Za-z0-9]*)([^\]]*)\]$",        RegexOptions.Compiled);
    private static readonly Regex RxClose      = new(@"^\[/([A-Za-z][A-Za-z0-9]*)\]$",               RegexOptions.Compiled);
    private static readonly Regex RxFenceOpen  = new(@"^(\s*)(```|~~~)(.*)?$",                        RegexOptions.Compiled);

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    // Stack frame for wrapping shortcodes
    private record Frame(string ComponentName, string DataParams, StringBuilder Accumulator, int OpenLineNo);

    public record Result(string ProcessedMarkdown, List<PartInfo> Parts);

    /// <summary>
    /// Processes shortcodes in the raw Markdown body and returns processed Markdown
    /// (with x-shortcode sentinels) plus any extracted [Part] entries.
    /// Throws <see cref="PublishException"/> for fatal shortcode errors.
    /// </summary>
    public static Result Process(string markdownBody, ComponentRegistry registry,
        string filePath, PublishOptions opts)
    {
        var lines = markdownBody.ReplaceLineEndings("\n").Split('\n');
        var root  = new StringBuilder();
        var stack = new Stack<Frame>();
        var parts = new List<PartInfo>();

        bool inFence = false;
        string fenceMarker = "";

        for (int lineNo = 1; lineNo <= lines.Length; lineNo++)
        {
            var line = lines[lineNo - 1];
            var trimmed = line.Trim();

            // ── Fenced code block tracking ──────────────────────────────────────
            if (!inFence)
            {
                var fm = RxFenceOpen.Match(trimmed);
                if (fm.Success)
                {
                    fenceMarker = fm.Groups[2].Value;
                    inFence = true;
                    Append(stack, root, line);
                    continue;
                }
            }
            else
            {
                if (trimmed == fenceMarker || trimmed.StartsWith(fenceMarker) &&
                    trimmed[fenceMarker.Length..].Trim().Length == 0)
                    inFence = false;
                Append(stack, root, line);
                continue;
            }

            // ── Self-closing shortcode ───────────────────────────────────────────
            var scm = RxSelfClose.Match(trimmed);
            if (scm.Success)
            {
                var name = scm.Groups[1].Value;
                if (registry.IsKnown(name))
                {
                    var dp = BuildDataParams(name, scm.Groups[2].Value, registry, filePath, lineNo);
                    var sentinel = $"<x-shortcode name=\"{name}\" data-params='{dp}'></x-shortcode>";
                    Append(stack, root, sentinel);
                    continue;
                }
                // Unknown → pass through as plain text
                Append(stack, root, line);
                continue;
            }

            // ── Inline wrapping shortcode ────────────────────────────────────────
            var im = RxInline.Match(trimmed);
            if (im.Success)
            {
                var name = im.Groups[1].Value;
                if (registry.IsKnown(name))
                {
                    var dp      = BuildDataParams(name, im.Groups[2].Value, registry, filePath, lineNo);
                    var inner   = Markdig.Markdown.ToHtml(im.Groups[3].Value.Trim(), Pipeline).Trim();
                    // Strip wrapping <p> that Markdig adds for single inline paragraphs
                    if (inner.StartsWith("<p>") && inner.EndsWith("</p>") && !inner[3..^4].Contains("<p>"))
                        inner = inner[3..^4];
                    var sentinel =
                        $"<x-shortcode name=\"{name}\" data-params='{dp}'>\n" +
                        $"  <!-- static content for crawlers -->\n" +
                        $"  {inner}\n" +
                        $"</x-shortcode>";
                    Append(stack, root, sentinel);
                    continue;
                }
                Append(stack, root, line);
                continue;
            }

            // ── Opening tag ──────────────────────────────────────────────────────
            var om = RxOpen.Match(trimmed);
            if (om.Success && trimmed != line.Trim()) { /* fall through if not on its own line */ }
            if (om.Success)
            {
                var name = om.Groups[1].Value;
                if (registry.IsKnown(name))
                {
                    var dp = BuildDataParams(name, om.Groups[2].Value, registry, filePath, lineNo);
                    stack.Push(new Frame(name, dp, new StringBuilder(), lineNo));
                    continue;
                }
                Append(stack, root, line);
                continue;
            }

            // ── Closing tag ──────────────────────────────────────────────────────
            var cm = RxClose.Match(trimmed);
            if (cm.Success)
            {
                var closeName = cm.Groups[1].Value;

                if (stack.Count == 0)
                    throw new PublishException(
                        $"{filePath}:{lineNo}: unexpected [/{closeName}] — no open shortcode.");

                var top = stack.Peek();
                if (!top.ComponentName.Equals(closeName, StringComparison.OrdinalIgnoreCase))
                    throw new PublishException(
                        $"{filePath}:{lineNo}: expected [/{top.ComponentName}] " +
                        $"(opened at line {top.OpenLineNo}), found [/{closeName}].");

                stack.Pop();
                var innerAccum = top.Accumulator.ToString().TrimEnd('\n');
                var innerHtml  = Markdig.Markdown.ToHtml(innerAccum, Pipeline).Trim();

                // [Part] special handling: extract and store, emit nothing into content
                if (closeName.Equals("Part", StringComparison.OrdinalIgnoreCase))
                {
                    if (stack.Count > 0)
                        throw new PublishException(
                            $"{filePath}:{lineNo}: [Part] may only appear at root level.");

                    // Extract Name= and ElementName= from the dataparams JSON
                    // (BuildDataParams already ran) — simpler to re-parse the raw params string
                    var partParams = ParseNamedParams(om.Success ? om.Groups[2].Value : "");
                    var partName = partParams.FirstOrDefault(p =>
                        p.Key.Equals("Name", StringComparison.OrdinalIgnoreCase)).Value ?? "part";
                    partName = partName.ToLowerInvariant();
                    var elemOverride = partParams.FirstOrDefault(p =>
                        p.Key.Equals("ElementName", StringComparison.OrdinalIgnoreCase)).Value;
                    var elemName = elemOverride ?? ElementNames.For(partName);

                    parts.Add(new PartInfo(partName, elemName, innerHtml));
                    continue;
                }

                // Normal wrapping shortcode → produce sentinel
                var sentinel =
                    $"<x-shortcode name=\"{top.ComponentName}\" data-params='{top.DataParams}'>\n" +
                    $"  <!-- static content for crawlers -->\n" +
                    $"  {innerHtml}\n" +
                    $"</x-shortcode>";

                Append(stack, root, sentinel);
                continue;
            }

            // ── Regular text line ────────────────────────────────────────────────
            Append(stack, root, line);
        }

        if (stack.Count > 0)
        {
            var top = stack.Peek();
            throw new PublishException(
                $"{filePath}: unclosed [{top.ComponentName}] opened at line {top.OpenLineNo}.");
        }

        return new Result(root.ToString(), parts);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static void Append(Stack<Frame> stack, StringBuilder root, string text)
    {
        if (stack.Count > 0) stack.Peek().Accumulator.AppendLine(text);
        else root.AppendLine(text);
    }

    /// <summary>Builds the data-params JSON for a shortcode sentinel from the raw params string.</summary>
    private static string BuildDataParams(string componentName, string rawParams,
        ComponentRegistry registry, string filePath, int lineNo)
    {
        var named  = new List<(string Key, string Value)>();
        var css    = new List<string>();

        foreach (var (key, value) in TokenizeParams(rawParams))
        {
            if (key != null) named.Add((key, value));
            else css.Add(value);
        }

        return registry.BuildDataParams(componentName, named, css, filePath, lineNo);
    }

    /// <summary>Returns just the named params (ignoring CSS class tokens) from a raw params string.</summary>
    private static List<(string Key, string Value)> ParseNamedParams(string rawParams)
    {
        var result = new List<(string, string)>();
        foreach (var (key, value) in TokenizeParams(rawParams))
            if (key != null) result.Add((key, value));
        return result;
    }

    /// <summary>
    /// Tokenizes the params string into (key, value) pairs for named params
    /// or (null, value) for CSS class tokens.
    /// </summary>
    private static IEnumerable<(string? Key, string Value)> TokenizeParams(string paramsStr)
    {
        paramsStr = paramsStr.Trim();
        int i = 0;
        while (i < paramsStr.Length)
        {
            while (i < paramsStr.Length && char.IsWhiteSpace(paramsStr[i])) i++;
            if (i >= paramsStr.Length) break;

            if (paramsStr[i] == '"')
            {
                // Quoted CSS class token: "value possible spaces"
                i++;
                var sb = new StringBuilder();
                while (i < paramsStr.Length && paramsStr[i] != '"') sb.Append(paramsStr[i++]);
                if (i < paramsStr.Length) i++; // closing "
                yield return (null, sb.ToString());
                continue;
            }

            // Read a word up to whitespace or '='
            int wordStart = i;
            while (i < paramsStr.Length && !char.IsWhiteSpace(paramsStr[i]) && paramsStr[i] != '=') i++;
            var word = paramsStr[wordStart..i];

            if (i < paramsStr.Length && paramsStr[i] == '=')
            {
                i++; // skip =
                string val;
                if (i < paramsStr.Length && paramsStr[i] == '"')
                {
                    i++; // skip opening "
                    var sb = new StringBuilder();
                    while (i < paramsStr.Length && paramsStr[i] != '"') sb.Append(paramsStr[i++]);
                    if (i < paramsStr.Length) i++; // skip closing "
                    val = sb.ToString();
                }
                else
                {
                    int valStart = i;
                    while (i < paramsStr.Length && !char.IsWhiteSpace(paramsStr[i])) i++;
                    val = paramsStr[valStart..i];
                }
                yield return (word, val);
            }
            else
            {
                // Bare CSS class token
                if (word.Length > 0) yield return (null, word);
            }
        }
    }
}
