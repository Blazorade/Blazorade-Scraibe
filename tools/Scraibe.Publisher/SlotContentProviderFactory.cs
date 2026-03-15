using Scraibe.Abstractions.Annotation;
using Scraibe.Abstractions.Content;
using System.Reflection;

namespace Scraibe.Publisher;

sealed class SlotContentProviderFactory
{
    private readonly Dictionary<string, ISlotContentProvider> _providers =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Discovers and instantiates slot content providers from the compiled component assembly.
    /// </summary>
    /// <param name="assemblyPath">Absolute path to the component library assembly used for reflection-based discovery.</param>
    public SlotContentProviderFactory(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            Console.Error.WriteLine($"Warning: component assembly not found at '{assemblyPath}'. Slot content provider discovery is disabled.");
            return;
        }

        var assembly = Assembly.LoadFrom(assemblyPath);
        Type[] types;
        try { types = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types.Where(t => t != null).ToArray()!;
        }

        foreach (var type in types)
        {
            if (!typeof(ISlotContentProvider).IsAssignableFrom(type)) continue;
            if (type.IsAbstract || type.IsInterface) continue;

            var providerName = type
                .GetCustomAttributes(typeof(ProviderNameAttribute), inherit: false)
                .Cast<ProviderNameAttribute>()
                .FirstOrDefault()
                ?.Name;

            if (string.IsNullOrWhiteSpace(providerName))
                continue;

            if (_providers.ContainsKey(providerName))
            {
                throw new PublishException(
                    $"Duplicate slot content provider name '{providerName}' found in '{assemblyPath}'.");
            }

            if (Activator.CreateInstance(type) is not ISlotContentProvider instance)
            {
                throw new PublishException(
                    $"Slot content provider '{type.FullName}' could not be instantiated. Providers must have a public parameterless constructor.");
            }

            _providers[providerName] = instance;
        }

        Console.WriteLine($"  Slot content providers: {_providers.Count} provider(s) loaded.");
    }

    /// <summary>
    /// Resolves a slot content provider by name and throws a publish error when the provider cannot be found.
    /// </summary>
    /// <param name="name">Provider name from slot metadata.</param>
    /// <param name="context">Human-readable context included in failures to simplify troubleshooting.</param>
    /// <returns>The resolved provider instance.</returns>
    /// <exception cref="PublishException">Thrown when no provider matches <paramref name="name"/>.</exception>
    public ISlotContentProvider ResolveOrThrow(string name, string context)
    {
        if (_providers.TryGetValue(name, out var provider))
            return provider;

        var available = _providers.Count == 0
            ? "(none loaded)"
            : string.Join(", ", _providers.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));

        throw new PublishException(
            $"Unknown slot content provider '{name}' ({context}). Available slot content providers: {available}.");
    }
}
