namespace Scraibe.Abstractions.Navigation;


/// <summary>
/// Represents an item in a navigation element rendered on a page. A navigation item typically represents a link in some sort of menu structure.
/// </summary>
public sealed class NavigationItem
{
    /// <summary>
    /// The title of the navigation item.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The URL that the navigation item points to.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// An optional description that can be shown in a tool-tip or on hover or something similar.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The CSS classes that represent the Bootstrap Icon to associate with the navigation item.
    /// </summary>
    public string? IconClass { get; set; }

    /// <summary>
    /// Defines whether the link represents the default page in a folder. This is typically a link that represents a `home.md` document.
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// A collection of child links to the current link.
    /// </summary>
    public List<NavigationItem> Children { get; set; } = new List<NavigationItem>();
}

/// <summary>
/// The model that represents a set of navigational links at a certain level.
/// </summary>
public sealed class NavigationModel
{
    /// <summary>
    /// The items to show in the navigation component that the model is sent to.
    /// </summary>
    public List<NavigationItem> Items { get; set; } = new List<NavigationItem>();

    /// <summary>
    /// A collection of ancestors representing higher levels, usually folders above the current folder.
    /// The first item in the collection is the closest ancestor, i.e. the parent, which is typically
    /// used in a link to get one level up. All ancestors can be used to create the breadcrumb path
    /// to the current folder level.
    /// </summary>
    public List<NavigationItem> Ancestors { get; set; } = new List<NavigationItem>();
}
