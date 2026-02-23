using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace {{WebAppName}}.Content;

/// <summary>
/// Parses an HTML string into an ordered tree of <see cref="ContentNode"/> instances
/// by walking an AngleSharp DOM tree and detecting <c>&lt;x-shortcode&gt;</c> sentinel
/// elements emitted by the publish pipeline.
/// </summary>
/// <remarks>
/// Block-level elements that contain no sentinel descendants are serialised back to HTML
/// and emitted as a single <see cref="HtmlNode"/> (rendered via <c>MarkupString</c>).
/// Block-level elements that contain one or more sentinel descendants are emitted as
/// <see cref="ElementNode"/> so that <c>ContentRenderer</c> can open a real element frame
/// in the Blazor render tree, insert the live component between the surrounding markup,
/// and close the frame — avoiding the partial-fragment problem that arises when
/// <c>MarkupString</c> (backed by <c>innerHTML</c>) is used to render unclosed tags.
/// <para>
/// SENTINEL CONTRACT: attribute quotes are normalised to double quotes and inner double
/// quotes are HTML-encoded as <c>&amp;quot;</c> by the time AngleSharp sees the string
/// (the browser's <c>DOMParser</c> does this before the string reaches this parser).
/// <c>DeserializeParameters</c> HTML-decodes the value before JSON parsing.
/// If the sentinel format changes, both the publisher and this parser must be updated together.
/// </para>
/// </remarks>
public static class ContentSegmentParser
{
    private static readonly HtmlParser HtmlParser = new();

    private const string ComponentAssembly = "{{ComponentLibraryName}}";
    private const string ComponentNamespace = "{{ComponentLibraryName}}.ShortCodes";

    /// <summary>
    /// Parses <paramref name="html"/> into a tree of <see cref="ContentNode"/> instances.
    /// </summary>
    public static List<ContentNode> Parse(string html)
    {
        var document = HtmlParser.ParseDocument(html);
        return ProcessDomNodes(document.Body!.ChildNodes);
    }

    // -------------------------------------------------------------------------
    // DOM walker
    // -------------------------------------------------------------------------

    private static List<ContentNode> ProcessDomNodes(INodeList domNodes)
    {
        var nodes = new List<ContentNode>();
        foreach (var domNode in domNodes)
            nodes.AddRange(ProcessDomNode(domNode));
        return nodes;
    }

    private static IEnumerable<ContentNode> ProcessDomNode(INode domNode)
    {
        // Text node — emit as HTML-encoded text so it is safe to inject via MarkupString.
        if (domNode is IText text)
        {
            if (!string.IsNullOrWhiteSpace(text.Data))
                yield return new HtmlNode { Html = WebUtility.HtmlEncode(text.Data) };
            yield break;
        }

        if (domNode is not IElement element)
            yield break; // skip comments, processing instructions, etc.

        // x-shortcode sentinel → ComponentNode (or flattened children for unknown components)
        if (element.LocalName == "x-shortcode")
        {
            var comp = BuildComponentNode(element);
            if (comp is not null)
                yield return comp;
            else
                foreach (var child in ProcessDomNodes(element.ChildNodes))
                    yield return child;
            yield break;
        }

        // Regular element containing at least one sentinel descendant → ElementNode.
        // ContentRenderer will use OpenElement/CloseElement to render it so that
        // live components can be inserted as proper siblings in the render tree.
        if (ContainsSentinel(element))
        {
            yield return new ElementNode
            {
                TagName = element.LocalName,
                Attributes = element.Attributes.ToDictionary(a => a.Name, a => a.Value),
                Children = ProcessDomNodes(element.ChildNodes)
            };
            yield break;
        }

        // Plain element with no sentinels — serialise back to HTML for MarkupString rendering.
        yield return new HtmlNode { Html = element.OuterHtml };
    }

    private static bool ContainsSentinel(INode node)
    {
        if (node is IElement e && e.LocalName == "x-shortcode")
            return true;
        foreach (var child in node.ChildNodes)
            if (ContainsSentinel(child))
                return true;
        return false;
    }

    private static ComponentNode? BuildComponentNode(IElement element)
    {
        var name = element.GetAttribute("name") ?? "";
        var paramsJson = element.GetAttribute("data-params") ?? "{}";
        var assemblyQualified = $"{ComponentNamespace}.{name}, {ComponentAssembly}";
        var type = Type.GetType(assemblyQualified);
        if (type is null) return null;

        return new ComponentNode
        {
            ComponentType = type,
            Parameters = DeserializeParameters(paramsJson, type),
            Children = ProcessDomNodes(element.ChildNodes)
        };
    }

    // -------------------------------------------------------------------------
    // Parameter deserialisation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Deserializes a JSON params string into a parameter dictionary, coercing each value
    /// to the declared type of the matching <c>[Parameter]</c> property on
    /// <paramref name="componentType"/>.
    /// </summary>
    private static Dictionary<string, object?> DeserializeParameters(string json, Type componentType)
    {
        var result = new Dictionary<string, object?>();

        // HTML-decode first: DOMParser normalises attribute quotes to double quotes and
        // HTML-encodes any inner double quotes in the JSON value to &quot;.
        var decoded = WebUtility.HtmlDecode(json);

        if (string.IsNullOrWhiteSpace(decoded) || decoded == "{}")
            return result;

        JsonDocument doc;
        try { doc = JsonDocument.Parse(decoded); }
        catch (JsonException) { return result; }

        // Build a case-insensitive lookup of [Parameter] properties on the component.
        var parameterProperties = componentType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<Microsoft.AspNetCore.Components.ParameterAttribute>() is not null)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var jsonProp in doc.RootElement.EnumerateObject())
        {
            if (!parameterProperties.TryGetValue(jsonProp.Name, out var propertyInfo))
                continue;

            try
            {
                var value = jsonProp.Value.Deserialize(propertyInfo.PropertyType);
                result[propertyInfo.Name] = value;
            }
            catch (JsonException)
            {
                // Type mismatch — skip this parameter rather than crashing.
            }
        }

        return result;
    }
}
