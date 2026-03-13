using Scraibe.Abstractions.Annotation;
using Scraibe.Abstractions.Navigation;
using System.Reflection;

namespace Scraibe.Publisher;

sealed class NavigationMarkupProviderFactory
{
    private readonly Dictionary<string, INavigationMarkupProvider> _providers =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Discovers and instantiates navigation markup providers from the compiled component assembly.
    /// </summary>
    /// <param name="assemblyPath">Absolute path to the component library assembly used for reflection-based discovery.</param>
    public NavigationMarkupProviderFactory(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            Console.Error.WriteLine($"Warning: component assembly not found at '{assemblyPath}'. Navigation provider discovery is disabled.");
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
            if (!typeof(INavigationMarkupProvider).IsAssignableFrom(type)) continue;
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
                    $"Duplicate navigation provider name '{providerName}' found in '{assemblyPath}'.");
            }

            if (Activator.CreateInstance(type) is not INavigationMarkupProvider instance)
            {
                throw new PublishException(
                    $"Navigation provider '{type.FullName}' could not be instantiated. Providers must have a public parameterless constructor.");
            }

            _providers[providerName] = instance;
        }

        Console.WriteLine($"  Navigation providers: {_providers.Count} provider(s) loaded.");
    }

    /// <summary>
    /// Resolves a provider by name and throws a publish error when the configured provider cannot be found.
    /// </summary>
    /// <param name="name">Provider name from layout metadata or effective configuration.</param>
    /// <param name="context">Human-readable context included in failures to simplify troubleshooting.</param>
    /// <returns>The resolved provider instance.</returns>
    /// <exception cref="PublishException">Thrown when no provider matches <paramref name="name"/>.</exception>
    public INavigationMarkupProvider ResolveOrThrow(string name, string context)
    {
        if (_providers.TryGetValue(name, out var provider))
            return provider;

        throw new PublishException(
            $"Unknown navigation provider '{name}' ({context}).");
    }
}