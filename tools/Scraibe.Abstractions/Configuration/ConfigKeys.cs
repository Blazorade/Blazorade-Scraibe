namespace Scraibe.Abstractions.Configuration;

/// <summary>
/// Canonical effective setting keys used in resolved configuration dictionaries.
/// Add new constants only when keys are implemented.
/// </summary>
public static class ConfigKeys
{
    /// <summary>
    /// Site display name used by UI and content features.
    /// Example value: <c>Blazorade</c>.
    /// </summary>
    public const string ScraibeSiteDisplayName = "scraibe.site.displayName";

    /// <summary>
    /// Technical app name used for project and namespace identity.
    /// Example value: <c>BlazoradeCom</c>.
    /// </summary>
    public const string ScraibeSiteAppName = "scraibe.site.appName";

    /// <summary>
    /// Host name for canonical URL generation and site identity.
    /// Example value: <c>blazorade.com</c>.
    /// </summary>
    public const string ScraibeSiteHostName = "scraibe.site.hostName";

    /// <summary>
    /// Repository-relative path to the web app project.
    /// Example value: <c>src/BlazoradeCom.Web</c>.
    /// </summary>
    public const string ScraibeSiteWebAppPath = "scraibe.site.webAppPath";

    /// <summary>
    /// Repository-relative path to the component library project.
    /// Example value: <c>src/BlazoradeCom.Components</c>.
    /// </summary>
    public const string ScraibeSiteComponentLibraryPath = "scraibe.site.componentLibraryPath";

    /// <summary>
    /// Content exclusion list consumed by publishing.
    /// Value is typically an array of content-relative paths.
    /// </summary>
    public const string ScraibePublishExcludedContent = "scraibe.publish.excludedContent";

    /// <summary>
    /// Default navigation provider name when layout placeholder does not define x-provider.
    /// </summary>
    public const string ScraibeNavigationProviderDefault = "scraibe.navigation.provider.default";

    /// <summary>
    /// Default slot content provider name when a non-navigation layout slot does not define x-provider.
    /// </summary>
    public const string ScraibeContentSlotProviderDefault = "scraibe.content.slot.provider.default";

    /// <summary>
    /// Number of descendant folder levels to include under navigation item children.
    /// Default is <c>1</c> when not configured.
    /// </summary>
    public const string ScraibeNavigationChildrenDepth = "scraibe.navigation.children.depth";

    /// <summary>
    /// Enables pinned navigation context inheritance from the folder where it is set.
    /// When enabled, descendants reuse that folder as their navigation context unless overridden.
    /// </summary>
    public const string ScraibeNavigationContextPinned = "scraibe.navigation.context.pinned";

    /// <summary>
    /// Default layout name to apply when a page does not specify one explicitly.
    /// </summary>
    public const string ScraibeLayoutDefault = "scraibe.layout.default";
}
