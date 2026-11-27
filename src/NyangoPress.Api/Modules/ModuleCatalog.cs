namespace NyangoPress.Api.Modules;

public static class ModuleCatalog
{
    /// <summary>Trouve et instancie tous les modules de l'assembly de l'API, triés par Order.</summary>
    public static IReadOnlyList<IModule> Discover() =>
        typeof(IModule).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IModule).IsAssignableFrom(t))
            .Select(t => (IModule)Activator.CreateInstance(t)!)
            .OrderBy(m => m.Order)
            .ThenBy(m => m.GetType().Name, StringComparer.Ordinal)
            .ToList();
}
