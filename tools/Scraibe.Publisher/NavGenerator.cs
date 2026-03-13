using Scraibe.Abstractions.Navigation;
using Scraibe.Abstractions.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Scraibe.Publisher;

/// <summary>
/// Builds normalized navigation models from published page metadata.
/// </summary>
static class NavGenerator
{
    /// <summary>
    /// Builds a folder-scoped navigation model for one page.
    /// </summary>
    public static NavigationModel BuildModelForPage(
        PageInfo currentPage,
        IReadOnlyList<PageInfo> allPages,
        string displayName,
        IReadOnlyDictionary<string, object?> effectiveConfiguration)
    {
        var currentFolderPath = GetFolderPath(currentPage.RelativePath);
        var stickyContextFolderPath = ResolveStickyContextFolderPath(currentPage);
        var navigationFolderPath = stickyContextFolderPath ?? currentFolderPath;
        var childrenDepth = GetChildrenDepth(effectiveConfiguration);

        var flatPages = allPages
            .Where(p => IsDirectChildOfFolder(p, navigationFolderPath) && !IsHome(p.RelativePath))
            .OrderBy(p => p.Frontmatter.Title)
            .ToList();

        var currentFolderHomePage = allPages
            .FirstOrDefault(p =>
                IsDirectChildOfFolder(p, navigationFolderPath)
                && IsHome(p.RelativePath));

        var childFolders = allPages
            .Select(p => GetImmediateChildFolder(p, navigationFolderPath))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var items = new List<NavigationItem>();

        if (currentFolderHomePage is not null && !string.IsNullOrWhiteSpace(navigationFolderPath))
        {
            items.Add(new NavigationItem
            {
                Title = currentFolderHomePage.Frontmatter.Title,
                Url = CleanUrl(currentFolderHomePage.Slug),
                IsDefault = true
            });
        }

        foreach (var page in flatPages)
        {
            items.Add(new NavigationItem
            {
                Title = page.Frontmatter.Title,
                Url = CleanUrl(page.Slug)
            });
        }

        foreach (var childFolderName in childFolders)
        {
            var childFolderPath = navigationFolderPath.Length == 0
                ? childFolderName
                : $"{navigationFolderPath}/{childFolderName}";
            items.Add(BuildFolderItem(childFolderName, childFolderPath, allPages, childrenDepth));
        }

        var ancestors = BuildAncestors(navigationFolderPath, allPages, displayName);

        return new NavigationModel
        {
            Items = items,
            Ancestors = ancestors
        };
    }

    private static NavigationItem BuildFolderItem(
        string folderSegment,
        string folderPath,
        IReadOnlyList<PageInfo> allPages,
        int depthRemaining)
    {
        var directChildPages = allPages
            .Where(p => IsDirectChildOfFolder(p, folderPath))
            .OrderBy(p => p.Frontmatter.Title)
            .ToList();

        var homePage = directChildPages.FirstOrDefault(p => IsHome(p.RelativePath));

        var folderItem = new NavigationItem
        {
            Title = homePage?.Frontmatter.Title ?? HumanizeFolderName(folderSegment),
            Url = homePage is not null ? CleanUrl(homePage.Slug) : string.Empty,
            IsDefault = homePage is not null
        };

        if (depthRemaining <= 0)
            return folderItem;

        foreach (var page in directChildPages.Where(p => !IsHome(p.RelativePath)))
        {
            folderItem.Children.Add(new NavigationItem
            {
                Title = page.Frontmatter.Title,
                Url = CleanUrl(page.Slug)
            });
        }

        var childFolders = allPages
            .Select(p => GetImmediateChildFolder(p, folderPath))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var childFolderName in childFolders)
        {
            var childFolderPath = $"{folderPath}/{childFolderName}";
            folderItem.Children.Add(BuildFolderItem(childFolderName, childFolderPath, allPages, depthRemaining - 1));
        }

        return folderItem;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static string GetFolderPath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').Trim('/');
        var slash = normalized.LastIndexOf('/');
        return slash < 0 ? "" : normalized[..slash];
    }

    private static bool IsDirectChildOfFolder(PageInfo page, string folderPath)
        => string.Equals(GetFolderPath(page.RelativePath), folderPath, StringComparison.OrdinalIgnoreCase);

    private static string? GetImmediateChildFolder(PageInfo page, string folderPath)
    {
        var pageFolderPath = GetFolderPath(page.RelativePath);
        if (string.Equals(pageFolderPath, folderPath, StringComparison.OrdinalIgnoreCase))
            return null;

        if (folderPath.Length == 0)
        {
            var slash = pageFolderPath.IndexOf('/');
            return slash < 0 ? pageFolderPath : pageFolderPath[..slash];
        }

        var prefix = folderPath + "/";
        if (!pageFolderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var remainder = pageFolderPath[prefix.Length..];
        if (remainder.Length == 0)
            return null;

        var slashInRemainder = remainder.IndexOf('/');
        return slashInRemainder < 0 ? remainder : remainder[..slashInRemainder];
    }

    private static bool IsHome(string relativeFileOrSegment)
        => Path.GetFileNameWithoutExtension(relativeFileOrSegment)
               .Equals("home", StringComparison.OrdinalIgnoreCase);

    private static int GetChildrenDepth(IReadOnlyDictionary<string, object?> effectiveConfiguration)
    {
        if (!effectiveConfiguration.TryGetValue(ConfigKeys.ScraibeNavigationChildrenDepth, out var raw)
            || raw is null)
        {
            return 1;
        }

        if (raw is int intValue)
            return Math.Max(0, intValue);

        if (raw is long longValue)
            return (int)Math.Max(0, Math.Min(int.MaxValue, longValue));

        if (raw is double doubleValue)
            return Math.Max(0, (int)Math.Floor(doubleValue));

        if (raw is string text && int.TryParse(text, out var parsed))
            return Math.Max(0, parsed);

        if (raw is JsonElement json)
        {
            if (json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out var jsonInt))
                return Math.Max(0, jsonInt);

            if (json.ValueKind == JsonValueKind.String
                && int.TryParse(json.GetString(), out var jsonParsed))
            {
                return Math.Max(0, jsonParsed);
            }
        }

        return 1;
    }

    private static string? ResolveStickyContextFolderPath(PageInfo currentPage)
    {
        var sourceFolderAbsolutePath = Path.GetDirectoryName(currentPage.SourcePath);
        if (string.IsNullOrWhiteSpace(sourceFolderAbsolutePath))
            return null;

        var contentRootAbsolutePath = ResolveContentRootAbsolutePath(currentPage);
        var repoRootAbsolutePath = Path.GetDirectoryName(contentRootAbsolutePath);
        if (string.IsNullOrWhiteSpace(repoRootAbsolutePath))
            return null;

        var chain = BuildFolderChain(sourceFolderAbsolutePath, repoRootAbsolutePath);

        var scoped = new StickySetting();
        var nearestLocal = new StickySetting();

        foreach (var folder in chain)
        {
            var configPath = Path.Combine(folder, ".config.json");
            if (!File.Exists(configPath))
                continue;

            var json = JsonNode.Parse(File.ReadAllText(configPath)) as JsonObject;
            if (json is null)
                continue;

            if (TryReadStickyValue(json["scoped"], out var scopedValue))
            {
                scoped = new StickySetting
                {
                    IsSet = true,
                    Value = scopedValue,
                    SourceFolderAbsolutePath = folder
                };
            }

            if (TryReadStickyValue(json["local"], out var localValue))
            {
                nearestLocal = new StickySetting
                {
                    IsSet = true,
                    Value = localValue,
                    SourceFolderAbsolutePath = folder
                };
            }
            else
            {
                nearestLocal = new StickySetting
                {
                    IsSet = false,
                    Value = false,
                    SourceFolderAbsolutePath = null
                };
            }
        }

        var effective = nearestLocal.IsSet ? nearestLocal : scoped;
        if (!effective.IsSet || !effective.Value || string.IsNullOrWhiteSpace(effective.SourceFolderAbsolutePath))
            return null;

        return ToContentRelativeFolderPath(effective.SourceFolderAbsolutePath!, contentRootAbsolutePath);
    }

    private static bool TryReadStickyValue(JsonNode? scopeNode, out bool value)
    {
        value = false;

        if (scopeNode is not JsonObject scope)
            return false;

        if (!TryGetPropertyIgnoreCase(scope, ConfigKeys.ScraibeNavigationContextPinned, out var stickyNode)
            || stickyNode is null)
        {
            return false;
        }

        if (stickyNode is JsonValue stickyValue)
        {
            if (stickyValue.TryGetValue<bool>(out var boolValue))
            {
                value = boolValue;
                return true;
            }

            if (stickyValue.TryGetValue<string>(out var text)
                && bool.TryParse(text, out var parsed))
            {
                value = parsed;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetPropertyIgnoreCase(JsonObject obj, string propertyName, out JsonNode? value)
    {
        if (obj.TryGetPropertyValue(propertyName, out value))
            return true;

        foreach (var kvp in obj)
        {
            if (string.Equals(kvp.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = kvp.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string ResolveContentRootAbsolutePath(PageInfo currentPage)
    {
        var relativePath = currentPage.RelativePath.Replace('/', Path.DirectorySeparatorChar);
        if (currentPage.SourcePath.EndsWith(relativePath, StringComparison.OrdinalIgnoreCase))
        {
            var prefixLength = currentPage.SourcePath.Length - relativePath.Length;
            return currentPage.SourcePath[..prefixLength]
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return Path.GetDirectoryName(currentPage.SourcePath) ?? string.Empty;
    }

    private static List<string> BuildFolderChain(string targetFolderAbsolutePath, string repoRootAbsolutePath)
    {
        var chain = new List<string>();
        var current = Path.GetFullPath(targetFolderAbsolutePath);
        var root = Path.GetFullPath(repoRootAbsolutePath);

        while (true)
        {
            chain.Add(current);
            if (string.Equals(current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                              root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                              StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrWhiteSpace(parent))
                break;

            current = parent;
        }

        chain.Reverse();
        return chain;
    }

    private static string ToContentRelativeFolderPath(string absoluteFolderPath, string contentRootAbsolutePath)
    {
        var relative = Path.GetRelativePath(contentRootAbsolutePath, absoluteFolderPath)
            .Replace('\\', '/')
            .Trim('/');

        return string.Equals(relative, ".", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : relative;
    }

    private static List<NavigationItem> BuildAncestors(
        string currentFolderPath,
        IReadOnlyList<PageInfo> allPages,
        string displayName)
    {
        var ancestors = new List<NavigationItem>();

        if (string.IsNullOrWhiteSpace(currentFolderPath))
            return ancestors;

        // Up-link semantics: navigate to the parent folder context, not the current folder.
        var cursor = GetParentFolderPath(currentFolderPath);
        while (cursor is not null)
        {
            if (cursor.Length == 0)
            {
                ancestors.Add(new NavigationItem
                {
                    Title = displayName,
                    Url = "/",
                    IsDefault = true
                });

                break;
            }

            var homePage = FindHomePage(allPages, cursor);
            if (homePage is not null)
            {
                ancestors.Add(new NavigationItem
                {
                    Title = homePage.Frontmatter.Title,
                    Url = "/" + cursor,
                    IsDefault = true
                });
            }

            cursor = GetParentFolderPath(cursor);
        }

        if (!ancestors.Any(a => a.Url == "/"))
        {
            ancestors.Add(new NavigationItem
            {
                Title = displayName,
                Url = "/",
                IsDefault = true
            });
        }

        return ancestors;
    }

    private static string? GetParentFolderPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        var normalized = folderPath.Replace('\\', '/').Trim('/');
        if (normalized.Length == 0)
            return null;

        var slash = normalized.LastIndexOf('/');
        return slash < 0 ? string.Empty : normalized[..slash];
    }

    private static PageInfo? FindHomePage(IReadOnlyList<PageInfo> allPages, string folderPath)
        => allPages.FirstOrDefault(p =>
            IsDirectChildOfFolder(p, folderPath) && IsHome(p.RelativePath));

    private sealed class StickySetting
    {
        public bool IsSet { get; set; }

        public bool Value { get; set; }

        public string? SourceFolderAbsolutePath { get; set; }
    }

    private static string HumanizeFolderName(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            return segment;

        var spaced = segment.Replace('-', ' ').Replace('_', ' ');
        return char.ToUpperInvariant(spaced[0]) + spaced[1..];
    }

    /// <summary>Converts a page slug into a clean URL (no .html, leading slash).</summary>
    private static string CleanUrl(string slug)
    {
        // home.md at any level maps to its parent directory clean URL
        if (slug.Equals("home", StringComparison.OrdinalIgnoreCase))
            return "/";
        // products/home → /products
        if (slug.EndsWith("/home", StringComparison.OrdinalIgnoreCase))
            return "/" + slug[..^5];
        return "/" + slug;
    }
}
