using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Scraibe.Publisher;

/// <summary>
/// Implements the stack-based shortcode parser described in publish.instructions.md.
/// Scans Markdown line-by-line, replaces known shortcodes with x-shortcode sentinel elements,
/// extracts [Part] blocks, and returns processed Markdown ready for Markdig conversion.
/// </summary>
static class ShortcodeProcessor
{
    // Regex patterns — used for fenced block detection only.
    private static readonly Regex RxFenceOpen  = new(@"^(\s*)(```+|~~~+)(.*)?$",                       RegexOptions.Compiled);

    // Stack frame for wrapping shortcodes. RawParams preserves opening-tag params
    // so [Part] can resolve Name/ElementName when the closing tag is encountered.
    private record Frame(string ComponentName, string DataParams, string RawParams, StringBuilder Accumulator, int OpenLineNo);

    private enum TagKind { Open, Close, Self }

    private readonly record struct TagToken(TagKind Kind, string Name, string RawParams, string Original, int EndExclusive);

    public record Result(string ProcessedMarkdown, List<PartInfo> Parts, IReadOnlyDictionary<string, string> ShortcodeInners);

    /// <summary>
    /// Processes shortcodes in the raw Markdown body and returns processed Markdown
    /// (with x-shortcode sentinels) plus any extracted [Part] entries.
    /// Throws <see cref="PublishException"/> for fatal shortcode errors.
    /// </summary>
    public static Result Process(string markdownBody, ComponentRegistry registry,
        string filePath, PublishOptions opts)
    {
        var lines      = markdownBody.ReplaceLineEndings("\n").Split('\n');
        var root       = new StringBuilder();
        var stack      = new Stack<Frame>();
        var parts      = new List<PartInfo>();
        var scInners   = new Dictionary<string, string>();
        int scInnerIdx = 0;

        bool inFence = false;
        string fenceMarker = "";
        bool inMermaidFence = false;
        var mermaidBody = new StringBuilder();

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
                    var langHint = fm.Groups[3].Value.Trim();
                    if (langHint.Equals("mermaid", StringComparison.OrdinalIgnoreCase))
                    {
                        inMermaidFence = true;
                        inFence = true;
                        mermaidBody.Clear();
                        // Do NOT emit the fence-open line — it becomes a sentinel instead
                        continue;
                    }
                    inFence = true;
                    Append(stack, root, line);
                    continue;
                }
            }
            else
            {
                var isClosingFence = trimmed == fenceMarker ||
                    (trimmed.StartsWith(fenceMarker) && trimmed[fenceMarker.Length..].Trim().Length == 0);

                if (inMermaidFence)
                {
                    if (!isClosingFence)
                    {
                        mermaidBody.AppendLine(line);
                        continue;
                    }
                    // Closing fence — emit Mermaid shortcode sentinel.
                    // Definition is serialised into data-params so that no child content
                    // is needed, which avoids the Blazor <!--!--> comment marker that
                    // Mermaid's parser trips over when ChildContent is used.
                    var definition = mermaidBody.ToString().Trim();
                    var dp = JsonSerializer.Serialize(new { Definition = definition });
                    var sentinel = $"<x-shortcode name=\"Mermaid\" data-params='{dp}'></x-shortcode>";
                    Append(stack, root, sentinel);
                    inMermaidFence = false;
                    inFence = false;
                    mermaidBody.Clear();
                    continue;
                }

                if (isClosingFence)
                    inFence = false;
                Append(stack, root, line);
                continue;
            }

            ProcessLine(line, stack, root, parts, scInners, registry, filePath, lineNo, ref scInnerIdx);
        }

        if (stack.Count > 0)
        {
            var top = stack.Peek();
            throw new PublishException(
                $"{filePath}: unclosed [{top.ComponentName}] opened at line {top.OpenLineNo}.");
        }

        return new Result(root.ToString(), parts, scInners);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static void ProcessLine(
        string line,
        Stack<Frame> stack,
        StringBuilder root,
        List<PartInfo> parts,
        Dictionary<string, string> scInners,
        ComponentRegistry registry,
        string filePath,
        int lineNo,
        ref int scInnerIdx)
    {
        var i = 0;
        while (i < line.Length)
        {
            // Inline code spans are literal: skip shortcode tokenization inside backticks.
            if (line[i] == '`')
            {
                int tickRun = 1;
                while (i + tickRun < line.Length && line[i + tickRun] == '`') tickRun++;

                var delim = new string('`', tickRun);
                var closeIdx = line.IndexOf(delim, i + tickRun, StringComparison.Ordinal);
                if (closeIdx < 0)
                {
                    AppendRaw(stack, root, line[i..]);
                    break;
                }

                AppendRaw(stack, root, line[i..(closeIdx + tickRun)]);
                i = closeIdx + tickRun;
                continue;
            }

            var openIdx = line.IndexOf('[', i);
            var tickIdx = line.IndexOf('`', i);
            if (tickIdx >= 0 && (openIdx < 0 || tickIdx < openIdx))
            {
                AppendRaw(stack, root, line[i..tickIdx]);
                i = tickIdx;
                continue;
            }

            if (openIdx < 0)
            {
                AppendRaw(stack, root, line[i..]);
                break;
            }

            var token = TryReadTagToken(line, openIdx);
            if (token is null || IsLinkLikeContext(line, openIdx, token.Value.EndExclusive))
            {
                if (openIdx > i)
                    AppendRaw(stack, root, line[i..openIdx]);
                AppendRaw(stack, root, "[");
                i = openIdx + 1;
                continue;
            }

            if (openIdx > i)
            {
                var between = line[i..openIdx];
                if (!string.IsNullOrWhiteSpace(between))
                    AppendRaw(stack, root, between);
            }

            var t = token.Value;
            switch (t.Kind)
            {
                case TagKind.Self:
                {
                    if (!registry.IsKnown(t.Name))
                    {
                        AppendRaw(stack, root, t.Original);
                        i = t.EndExclusive;
                        continue;
                    }

                    var dp = BuildDataParams(t.Name, t.RawParams, registry, filePath, lineNo);
                    var sentinel = $"<x-shortcode name=\"{t.Name}\" data-params='{dp}'></x-shortcode>";
                    AppendRaw(stack, root, sentinel);
                    i = t.EndExclusive;
                    continue;
                }
                case TagKind.Open:
                {
                    if (!registry.IsKnown(t.Name))
                    {
                        AppendRaw(stack, root, t.Original);
                        i = t.EndExclusive;
                        continue;
                    }

                    var dp = BuildDataParams(t.Name, t.RawParams, registry, filePath, lineNo);
                    stack.Push(new Frame(t.Name, dp, t.RawParams, new StringBuilder(), lineNo));
                    i = t.EndExclusive;
                    continue;
                }
                case TagKind.Close:
                {
                    if (stack.Count == 0)
                        throw new PublishException(
                            $"{filePath}:{lineNo}: unexpected [/{t.Name}] — no open shortcode.");

                    var top = stack.Peek();
                    if (!top.ComponentName.Equals(t.Name, StringComparison.OrdinalIgnoreCase))
                        throw new PublishException(
                            $"{filePath}:{lineNo}: expected [/{top.ComponentName}] " +
                            $"(opened at line {top.OpenLineNo}), found [/{t.Name}].");

                    stack.Pop();
                    var innerAccum = top.Accumulator.ToString().TrimEnd('\n');
                    var normalizedInner = NormalizeInnerMarkdown(innerAccum);
                    var innerHtml = MarkdownRenderer.ToHtml(normalizedInner).Trim();

                    // Inline wrappers should keep previous behavior where a single paragraph
                    // does not force an extra <p> wrapper around simple text content.
                    if (lineNo == top.OpenLineNo && innerHtml.StartsWith("<p>", StringComparison.Ordinal) &&
                        innerHtml.EndsWith("</p>", StringComparison.Ordinal) && !innerHtml[3..^4].Contains("<p>", StringComparison.Ordinal))
                    {
                        innerHtml = innerHtml[3..^4];
                    }

                    // [Part] special handling: extract and store, emit nothing into content
                    if (t.Name.Equals("Part", StringComparison.OrdinalIgnoreCase))
                    {
                        if (stack.Count > 0)
                            throw new PublishException(
                                $"{filePath}:{lineNo}: [Part] may only appear at root level.");

                        innerHtml = ReplaceShortcodeInnerPlaceholders(innerHtml, scInners);

                        var partParams = ParseNamedParams(top.RawParams);
                        var partName = partParams.FirstOrDefault(p =>
                            p.Key.Equals("Name", StringComparison.OrdinalIgnoreCase)).Value ?? "part";
                        partName = partName.ToLowerInvariant();
                        var elemOverride = partParams.FirstOrDefault(p =>
                            p.Key.Equals("ElementName", StringComparison.OrdinalIgnoreCase)).Value;
                        var elemName = elemOverride ?? ElementNames.For(partName);

                        parts.Add(new PartInfo(partName, elemName, innerHtml));
                        i = t.EndExclusive;
                        continue;
                    }

                    var placeholder = $"x-sc-inner-{scInnerIdx++}";
                    scInners[placeholder] =
                        $"  <!-- static content for crawlers -->\n" +
                        $"  {innerHtml}";
                    var sentinel =
                        $"<x-shortcode name=\"{top.ComponentName}\" data-params='{top.DataParams}'>\n" +
                        $"<!--{placeholder}-->\n" +
                        $"</x-shortcode>";

                    AppendRaw(stack, root, sentinel);
                    i = t.EndExclusive;
                    continue;
                }
            }
        }

        AppendRaw(stack, root, "\n");
    }

    private static void Append(Stack<Frame> stack, StringBuilder root, string text)
    {
        if (stack.Count > 0) stack.Peek().Accumulator.AppendLine(text);
        else root.AppendLine(text);
    }

    private static void AppendRaw(Stack<Frame> stack, StringBuilder root, string text)
    {
        if (stack.Count > 0) stack.Peek().Accumulator.Append(text);
        else root.Append(text);
    }

    private static TagToken? TryReadTagToken(string line, int openIdx)
    {
        if (line[openIdx] != '[') return null;

        var closeIdx = line.IndexOf(']', openIdx + 1);
        if (closeIdx < 0) return null;

        var inner = line[(openIdx + 1)..closeIdx];
        if (inner.Length == 0) return null;

        if (inner[0] == '/')
        {
            var closeName = inner[1..].Trim();
            if (!IsShortcodeName(closeName)) return null;
            return new TagToken(
                TagKind.Close,
                closeName,
                "",
                line[openIdx..(closeIdx + 1)],
                closeIdx + 1);
        }

        var tagBody = inner;
        var tagBodyTrimmedEnd = tagBody.TrimEnd();
        var isSelf = tagBodyTrimmedEnd.EndsWith("/", StringComparison.Ordinal);
        if (isSelf)
            tagBody = tagBodyTrimmedEnd[..^1];

        var span = tagBody.AsSpan();
        var j = 0;
        while (j < span.Length && char.IsWhiteSpace(span[j])) j++;
        if (j >= span.Length || !char.IsLetter(span[j])) return null;

        var start = j;
        j++;
        while (j < span.Length && char.IsLetterOrDigit(span[j])) j++;
        var name = span[start..j].ToString();
        if (!IsShortcodeName(name)) return null;

        var rawParams = j < span.Length ? span[j..].ToString() : "";
        return new TagToken(
            isSelf ? TagKind.Self : TagKind.Open,
            name,
            rawParams,
            line[openIdx..(closeIdx + 1)],
            closeIdx + 1);
    }

    private static bool IsShortcodeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (!char.IsLetter(name[0])) return false;
        for (int i = 1; i < name.Length; i++)
            if (!char.IsLetterOrDigit(name[i])) return false;
        return true;
    }

    private static bool IsLinkLikeContext(string line, int openIdx, int endExclusive)
    {
        // Markdown links/images should not be tokenized as shortcodes.
        if (openIdx > 0 && line[openIdx - 1] == '!') return true;
        if (endExclusive < line.Length && line[endExclusive] == '(') return true;
        return false;
    }

    private static string NormalizeInnerMarkdown(string inner)
    {
        if (string.IsNullOrEmpty(inner)) return inner;

        var lines = inner.ReplaceLineEndings("\n").Split('\n');
        var normalized = lines.Select(NormalizeLeadingTabs).ToArray();

        int? minIndent = null;
        foreach (var ln in normalized)
        {
            if (string.IsNullOrWhiteSpace(ln)) continue;
            int indent = 0;
            while (indent < ln.Length && ln[indent] == ' ') indent++;
            minIndent = minIndent is null ? indent : Math.Min(minIndent.Value, indent);
            if (minIndent == 0) break;
        }

        if (minIndent is null || minIndent.Value == 0)
            return string.Join("\n", normalized);

        int remove = minIndent.Value;
        for (int i = 0; i < normalized.Length; i++)
        {
            var ln = normalized[i];
            if (ln.Length == 0) continue;
            if (ln.Length >= remove) normalized[i] = ln[remove..];
            else normalized[i] = "";
        }

        return string.Join("\n", normalized);
    }

    private static string NormalizeLeadingTabs(string line)
    {
        if (line.Length == 0) return line;

        var sb = new StringBuilder(line.Length);
        int i = 0;
        while (i < line.Length)
        {
            var ch = line[i];
            if (ch == '\t')
            {
                sb.Append("    ");
                i++;
                continue;
            }
            if (ch == ' ')
            {
                sb.Append(' ');
                i++;
                continue;
            }
            break;
        }
        if (i < line.Length) sb.Append(line[i..]);
        return sb.ToString();
    }

    private static string ReplaceShortcodeInnerPlaceholders(string html, IReadOnlyDictionary<string, string> scInners)
    {
        bool changed;
        do
        {
            changed = false;
            foreach (var (placeholder, inner) in scInners)
            {
                var marker = $"<!--{placeholder}-->";
                if (!html.Contains(marker, StringComparison.Ordinal))
                    continue;

                html = html.Replace(marker, inner, StringComparison.Ordinal);
                changed = true;
            }
        }
        while (changed);

        return html;
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
