using System.Text.Json;
using Scraibe.Abstractions.Configuration;

namespace Scraibe.Publisher;

using JsonDictionary = Dictionary<string, object?>;

/// <summary>
/// Resolves effective folder settings from .config.json files across the repository.
/// </summary>
sealed class FolderConfigParser
{
    private const string ConfigFileName = ".config.json";
    private readonly string _repoRootPath;

    public FolderConfigParser(string repositoryRootPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryRootPath))
            throw new ArgumentException("Repository root path is required.", nameof(repositoryRootPath));

        _repoRootPath = Path.GetFullPath(repositoryRootPath);
        if (!Directory.Exists(_repoRootPath))
            throw new PublishException($"Repository root folder not found: '{_repoRootPath}'.");
    }

    /// <summary>
    /// Accepts one repository-relative path (file or folder) and returns effective settings
    /// for the resolved target folder.
    /// </summary>
    public JsonDictionary GetEffectiveSettings(string repositoryRelativePath)
    {
        if (string.IsNullOrWhiteSpace(repositoryRelativePath))
            throw new PublishException("Repository-relative path is required.");

        var targetAbsolutePath = ResolveRepositoryRelativePath(repositoryRelativePath);
        var targetFolderPath = ResolveTargetFolderPath(targetAbsolutePath, repositoryRelativePath);

        var effective = new JsonDictionary(StringComparer.OrdinalIgnoreCase);
        var folderChain = BuildFolderChain(targetFolderPath);
        FolderConfig? nearestLocalConfig = null;

        for (var i = 0; i < folderChain.Count; i++)
        {
            var folder = folderChain[i];
            var configFilePath = Path.Combine(folder, ConfigFileName);
            if (!File.Exists(configFilePath))
                continue;

            var cfg = LoadAndValidateConfig(configFilePath);

            Merge(cfg.Scoped, effective);
            nearestLocalConfig = cfg;
        }

        // Apply "local" from the closest folder in the chain that has a .config.json.
        // If the target folder has no .config.json, the nearest parent's local settings
        // become the effective local layer for this target.
        if (nearestLocalConfig is not null)
            Merge(nearestLocalConfig.Local, effective);

        return effective;
    }

    private string ResolveRepositoryRelativePath(string repositoryRelativePath)
    {
        var normalized = repositoryRelativePath.Replace('\\', '/').Trim();
        normalized = normalized.TrimStart('/');

        if (normalized.StartsWith("../", StringComparison.Ordinal)
            || normalized.Equals("..", StringComparison.Ordinal)
            || Path.IsPathRooted(normalized))
        {
            throw new PublishException(
                $"Invalid repository-relative path '{repositoryRelativePath}'.");
        }

        var absolutePath = Path.GetFullPath(Path.Combine(_repoRootPath, normalized));
        if (!IsPathInsideRepo(absolutePath))
            throw new PublishException(
                $"Resolved path '{absolutePath}' is outside repository root '{_repoRootPath}'.");

        return absolutePath;
    }

    private string ResolveTargetFolderPath(string absolutePath, string originalInput)
    {
        if (Directory.Exists(absolutePath))
            return absolutePath;

        if (File.Exists(absolutePath))
            return Path.GetDirectoryName(absolutePath) ?? _repoRootPath;

        throw new PublishException(
            $"Repository-relative path '{originalInput}' does not exist at '{absolutePath}'.");
    }

    private List<string> BuildFolderChain(string targetFolderPath)
    {
        var chain = new List<string>();
        var current = Path.GetFullPath(targetFolderPath);

        if (!IsPathInsideRepo(current))
            throw new PublishException(
                $"Target folder '{current}' is outside repository root '{_repoRootPath}'.");

        while (true)
        {
            chain.Add(current);
            if (PathEquals(current, _repoRootPath))
                break;

            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrWhiteSpace(parent))
                throw new PublishException(
                    $"Unable to resolve folder chain from '{targetFolderPath}' to repository root '{_repoRootPath}'.");

            current = parent;
        }

        chain.Reverse();
        return chain;
    }

    private static FolderConfig LoadAndValidateConfig(string configFilePath)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(configFilePath));
        }
        catch (Exception ex)
        {
            throw new PublishException($"Invalid JSON in '{configFilePath}': {ex.Message}");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new PublishException(
                    $"Invalid .config.json at '{configFilePath}': document root must be a JSON object.");
            }

            var root = document.RootElement;
            var local = ReadScopeObject(root, "local", configFilePath);
            var scoped = ReadScopeObject(root, "scoped", configFilePath);

            ValidateNoOverlappingKeys(local, scoped, configFilePath);

            return new FolderConfig
            {
                Local = local,
                Scoped = scoped
            };
        }
    }

    private static JsonDictionary ReadScopeObject(JsonElement root, string propertyName, string configFilePath)
    {
        if (!root.TryGetProperty(propertyName, out var prop))
            return new JsonDictionary(StringComparer.OrdinalIgnoreCase);

        if (prop.ValueKind != JsonValueKind.Object)
        {
            throw new PublishException(
                $"Invalid .config.json at '{configFilePath}': {propertyName} must be an object.");
        }

        var result = new JsonDictionary(StringComparer.OrdinalIgnoreCase);
        foreach (var child in prop.EnumerateObject())
            result[child.Name] = ReadJsonValue(child.Value);

        return result;
    }

    private static object? ReadJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => value.EnumerateObject()
                .ToDictionary(x => x.Name, x => ReadJsonValue(x.Value), StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => value.EnumerateArray().Select(ReadJsonValue).ToList(),
            _ => value.ToString()
        };
    }

    private static void ValidateNoOverlappingKeys(
        JsonDictionary local,
        JsonDictionary scoped,
        string configFilePath)
    {
        foreach (var key in local.Keys)
        {
            if (scoped.ContainsKey(key))
            {
                throw new PublishException(
                    $"Invalid .config.json at '{configFilePath}': key '{key}' appears in both local and scoped.");
            }
        }
    }

    private static void Merge(JsonDictionary source, JsonDictionary target)
    {
        foreach (var kvp in source)
            target[kvp.Key] = kvp.Value;
    }

    private bool IsPathInsideRepo(string absolutePath)
    {
        var candidate = EnsureTrailingSeparator(Path.GetFullPath(absolutePath));
        var root = EnsureTrailingSeparator(_repoRootPath);
        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            || PathEquals(absolutePath, _repoRootPath);
    }

    private static bool PathEquals(string left, string right)
        => string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }
}
