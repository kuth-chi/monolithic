using System.Reflection;
using Microsoft.Extensions.Logging;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Platform.Core.Infrastructure;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Auto-discovers all <see cref="IModule"/> implementations in the given
/// assemblies, sorts them by dependency order (topological sort), and
/// orchestrates service registration and pipeline configuration.
///
/// Plug-and-play workflow:
///   1. Create a class that implements <see cref="IModule"/>.
///   2. The registry finds it automatically at startup — no manual wiring needed.
///   3. Modules declare optional <see cref="IModule.Dependencies"/> to control order.
///
/// Thread-safety: registration happens once at startup; the module list is
/// immutable after <see cref="Discover"/>.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class ModuleRegistry
{
    private readonly List<IModule> _modules = [];
    private readonly ILogger<ModuleRegistry> _logger;

    public ModuleRegistry(ILogger<ModuleRegistry> logger) => _logger = logger;

    /// <summary>Read-only ordered list of registered modules.</summary>
    public IReadOnlyList<IModule> Modules => _modules.AsReadOnly();

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Scans <paramref name="assemblies"/> for concrete, non-abstract types
    /// that implement <see cref="IModule"/>, instantiates them via their
    /// parameterless constructor, topologically sorts them, and registers
    /// services + pipeline for each.
    /// </summary>
    public void Discover(
        IServiceCollection services,
        IConfiguration configuration,
        WebApplication? app,
        params Assembly[] assemblies)
    {
        var discovered = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.IsAssignableTo(typeof(IModule)))
            .Select(t => (IModule)Activator.CreateInstance(t)!)
            .ToDictionary(m => m.ModuleId);

        _logger.LogInformation("[ModuleRegistry] Discovered {Count} module(s): {Ids}",
            discovered.Count,
            string.Join(", ", discovered.Keys));

        var sorted = TopologicalSort(discovered);

        foreach (var module in sorted)
        {
            _logger.LogInformation("[ModuleRegistry] Registering module: {Id} v{Version}",
                module.ModuleId, module.Version);

            module.RegisterServices(services, configuration);

            if (app is not null)
                module.ConfigurePipeline(app);

            _modules.Add(module);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Configures pipelines for all registered modules.</summary>
    public void ConfigureAll(WebApplication app)
    {
        foreach (var module in _modules)
            module.ConfigurePipeline(app);
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Returns all <see cref="WidgetDescriptor"/> entries from every module.
    /// Used to seed the widget catalog on first startup.
    /// </summary>
    public IEnumerable<WidgetDescriptor> GetAllWidgets()
        => _modules.SelectMany(m => m.GetWidgets());

    /// <summary>
    /// Returns all <see cref="DefaultTemplateDescriptor"/> entries from every module.
    /// </summary>
    public IEnumerable<DefaultTemplateDescriptor> GetAllDefaultTemplates()
        => _modules.SelectMany(m => m.GetDefaultTemplates());

    // ─────────────────────────────────────────────────────────────────────────
    // Kahn's algorithm – stable topological sort by dependency order.
    // ─────────────────────────────────────────────────────────────────────────
    private static List<IModule> TopologicalSort(Dictionary<string, IModule> modules)
    {
        var inDegree = modules.Keys.ToDictionary(k => k, _ => 0);
        var adj = modules.Keys.ToDictionary(k => k, _ => new List<string>());

        foreach (var (id, module) in modules)
        {
            foreach (var dep in module.Dependencies)
            {
                if (!modules.ContainsKey(dep))
                    throw new InvalidOperationException(
                        $"Module '{id}' declares dependency '{dep}' which is not registered.");

                adj[dep].Add(id);
                inDegree[id]++;
            }
        }

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = new List<IModule>(modules.Count);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(modules[current]);

            foreach (var next in adj[current])
            {
                if (--inDegree[next] == 0)
                    queue.Enqueue(next);
            }
        }

        if (sorted.Count != modules.Count)
            throw new InvalidOperationException(
                "Circular module dependency detected. Check IModule.Dependencies declarations.");

        return sorted;
    }
}
