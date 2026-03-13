namespace Scraibe.Abstractions.Annotation;

/// <summary>
/// An attribute that is used to mark a provider implementation with.
/// </summary>
/// <remarks>
/// In addition to marking the provider with this attribute, you must also implement
/// one or more service interfaces to define the capabilities of the provider.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class ProviderNameAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with the provider lookup name used in configuration.
    /// </summary>
    /// <param name="name">The case-insensitive provider name token.</param>
    public ProviderNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the provider lookup name declared on the target type.
    /// </summary>
    public string Name { get; }
}
