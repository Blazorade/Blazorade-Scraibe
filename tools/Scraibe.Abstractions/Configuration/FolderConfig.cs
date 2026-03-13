namespace Scraibe.Abstractions.Configuration;

/// <summary>
/// Represents one parsed .config.json payload with local and scoped setting dictionaries.
/// </summary>
public class FolderConfig
{
    /// <summary>
    /// Gets or sets settings that apply only to the folder where the config file is defined.
    /// </summary>
    public Dictionary<string, object?> Local { get; set; } = new();

    /// <summary>
    /// Gets or sets settings that apply to the folder and all descendant folders.
    /// </summary>
    public Dictionary<string, object?> Scoped { get; set; } = new();
}
