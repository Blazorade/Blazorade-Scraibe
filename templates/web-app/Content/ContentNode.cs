namespace {{WebAppName}}.Content;

/// <summary>Base class for nodes in the parsed content tree produced by <see cref="ContentSegmentParser"/>.</summary>
public abstract class ContentNode { }

/// <summary>
/// A leaf node containing raw HTML to be rendered as a <c>MarkupString</c>.
/// </summary>
public sealed class HtmlNode : ContentNode
{
    public string Html { get; init; } = string.Empty;
}

/// <summary>
/// A node representing a Blazor component to be rendered via <c>DynamicComponent</c>.
/// Child nodes, if any, are composed into a <c>ChildContent</c> <c>RenderFragment</c>
/// and passed as a parameter to the component.
/// </summary>
public sealed class ComponentNode : ContentNode
{
    public Type ComponentType { get; init; } = typeof(object);
    public Dictionary<string, object?> Parameters { get; init; } = [];

    /// <summary>
    /// Ordered child nodes nested inside this component's shortcode block.
    /// Empty for self-closing shortcodes.
    /// </summary>
    public List<ContentNode> Children { get; init; } = [];
}

/// <summary>
/// Represents an HTML element that contains at least one <see cref="ComponentNode"/> descendant,
/// requiring it to be rendered via <c>OpenElement</c>/<c>CloseElement</c> rather than as a
/// <c>MarkupString</c>, so the components can be inserted as proper siblings in the render tree.
/// </summary>
public sealed class ElementNode : ContentNode
{
    public string TagName { get; init; } = "";
    public Dictionary<string, string> Attributes { get; init; } = [];
    public List<ContentNode> Children { get; init; } = [];
}
