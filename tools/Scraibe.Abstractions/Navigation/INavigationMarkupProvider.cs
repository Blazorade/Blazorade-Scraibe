namespace Scraibe.Abstractions.Navigation;

/// <summary>
/// Defines a pluggable renderer that turns a normalized navigation model into HTML markup.
/// </summary>
public interface INavigationMarkupProvider
{
    /// <summary>
    /// Creates provider-specific navigation markup for the current page context.
    /// </summary>
    /// <param name="model">The normalized navigation model prepared by the publisher.</param>
    /// <param name="effectiveConfiguration">Effective folder configuration for provider-specific decisions.</param>
    /// <returns>HTML markup for the provider's root navigation element.</returns>
    string CreateMarkup(NavigationModel model, IReadOnlyDictionary<string, object?> effectiveConfiguration);
}
