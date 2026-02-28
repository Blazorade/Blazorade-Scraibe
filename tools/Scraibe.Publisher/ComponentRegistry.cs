using System.Reflection;
using System.Text.Json;

namespace Scraibe.Publisher;

/// <summary>
/// Loads the compiled component library assembly and builds a registry of shortcode component
/// types found in the ShortCodes namespace, together with their [Parameter] properties.
/// Used at publish time to resolve shortcode names and serialise data-params JSON.
/// </summary>
class ComponentRegistry
{
    private record ComponentInfo(string CanonicalName,
        // key: lowercased param name  →  value: canonical property name
        Dictionary<string, string> Parameters,
        Dictionary<string, Type> ParameterTypes);

    private readonly Dictionary<string, ComponentInfo> _components =
        new(StringComparer.OrdinalIgnoreCase);

    public bool Loaded { get; private set; }

    public ComponentRegistry(string assemblyPath, string shortcodesNamespace)
    {
        if (!File.Exists(assemblyPath))
        {
            Console.Error.WriteLine($"Warning: component assembly not found at '{assemblyPath}'. " +
                "Shortcode component validation is disabled.");
            return;
        }

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e)
            { types = e.Types.Where(t => t != null).ToArray()!; }

            foreach (var type in types)
            {
                if (type.Namespace != shortcodesNamespace) continue;

                var paramMap  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var typeMap   = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CustomAttributes.Any(a =>
                        a.AttributeType.FullName == "Microsoft.AspNetCore.Components.ParameterAttribute"))
                        continue;
                    paramMap[prop.Name.ToLowerInvariant()] = prop.Name;
                    typeMap[prop.Name] = prop.PropertyType;
                }

                _components[type.Name] = new ComponentInfo(type.Name, paramMap, typeMap);
            }

            Loaded = true;
            Console.WriteLine($"  Component registry: {_components.Count} shortcode component(s) loaded.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: could not load component assembly: {ex.Message}");
        }
    }

    /// <summary>Returns true if the component name is a known ShortCode component.</summary>
    public bool IsKnown(string name) => _components.ContainsKey(name);

    /// <summary>
    /// Builds the data-params JSON string for a shortcode sentinel element.
    /// Named params are matched case-insensitively to canonical property names.
    /// CSS class tokens are joined and stored as the CssClasses parameter.
    /// </summary>
    public string BuildDataParams(
        string componentName,
        List<(string Key, string Value)> namedParams,
        List<string> cssClassTokens,
        string filePath,
        int lineNumber)
    {
        var obj = new Dictionary<string, object>(StringComparer.Ordinal);

        // Detect duplicate named params after case normalisation
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, _) in namedParams)
        {
            if (!seen.Add(key))
                throw new PublishException(
                    $"{filePath}:{lineNumber}: duplicate parameter '{key}' on [{componentName}].");
        }

        // CSS class tokens → CssClasses
        string? cssValue = cssClassTokens.Count > 0 ? string.Join(' ', cssClassTokens) : null;

        foreach (var (key, value) in namedParams)
        {
            // Skip explicit CssClasses if we also have CSS class tokens (tokens win)
            if (key.Equals("CssClasses", StringComparison.OrdinalIgnoreCase) && cssValue != null)
            {
                Console.Error.WriteLine(
                    $"Warning: {filePath}:{lineNumber}: explicit CssClasses discarded because " +
                    $"CSS class tokens are present on [{componentName}].");
                continue;
            }

            var canonical = ResolveParamName(componentName, key) ?? key;
            obj[canonical] = CoerceValue(componentName, canonical, value);
        }

        if (cssValue != null)
            obj[ResolveParamName(componentName, "CssClasses") ?? "CssClasses"] = cssValue;

        return obj.Count == 0 ? "{}" : JsonSerializer.Serialize(obj);
    }

    /// <summary>Resolves a parameter name (case-insensitive) to its canonical declared form.</summary>
    private string? ResolveParamName(string componentName, string paramName)
    {
        if (!_components.TryGetValue(componentName, out var info)) return null;
        return info.Parameters.TryGetValue(paramName.ToLowerInvariant(), out var canonical)
            ? canonical : null;
    }

    /// <summary>Coerces a string value to bool, long, or double if the property type dictates it.</summary>
    private object CoerceValue(string componentName, string canonicalPropName, string value)
    {
        if (_components.TryGetValue(componentName, out var info) &&
            info.ParameterTypes.TryGetValue(canonicalPropName, out var propType))
        {
            var underlying = Nullable.GetUnderlyingType(propType) ?? propType;
            if (underlying == typeof(bool) && bool.TryParse(value, out var b)) return b;
            if (underlying == typeof(int)  && int.TryParse(value, out var i))  return i;
            if (underlying == typeof(long) && long.TryParse(value, out var l)) return l;
            if (underlying == typeof(double) &&
                double.TryParse(value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
        }
        else
        {
            // Heuristic coercion when type info is unavailable
            if (bool.TryParse(value, out var b)) return b;
            if (long.TryParse(value, out var l)) return l;
            if (double.TryParse(value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
        }
        return value;
    }
}

/// <summary>A fatal publish-time error that stops the current page or the entire run.</summary>
class PublishException(string message) : Exception(message);
