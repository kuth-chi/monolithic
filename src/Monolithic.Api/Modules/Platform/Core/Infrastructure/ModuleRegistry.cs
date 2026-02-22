using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Platform.Core.Infrastructure;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Auto-discovers all <see cref="IModule"/> implementations, sorts them by
/// dependency order (Kahn topological sort), and orchestrates:
///
///   Phase 1 — DI Build  : <see cref="Discover"/> — called from AddApiServices()
///     • Scans assemblies for IModule implementations via reflection
///     • Calls RegisterServices() on each module (DRY: no manual service wiring)
///     • Seeds ASP.NET Core <see cref="AuthorizationOptions"/> from all permissions
///
///   Phase 2 — Pipeline  : <see cref="ConfigureAll"/> — called from UseApiPipeline()
///     • Calls ConfigurePipeline() on each registered module
///
///   Phase 3 — Startup   : InitializePlatformAsync (PlatformStartup.cs)
///     • Calls OnFirstRunAsync() on each module once per installation
///
/// Plug-and-play workflow:
///   1. Create a class implementing <see cref="IModule"/> (or extend ModuleBase).
///   2. The registry auto-discovers it — zero startup wiring needed.
///   3. Declare <see cref="IModule.Dependencies"/> to control registration order.
///
/// Thread-safety: the module list is immutable after <see cref="Discover"/> returns.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class ModuleRegistry
{
    private readonly List<IModule> _modules = [];
    private ILogger<ModuleRegistry> _logger;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Parameterless constructor used when the registry is created BEFORE the
    /// DI container is built (i.e. during AddApiServices).  Uses NullLogger.
    /// Logger is upgraded via <see cref="SetLogger"/> after host build.
    /// </summary>
    public ModuleRegistry()
        => _logger = NullLogger<ModuleRegistry>.Instance;

    // Kept for DI injection in tests / when registry is resolved after build.
    public ModuleRegistry(ILogger<ModuleRegistry> logger) => _logger = logger;

    /// <summary>Replace the NullLogger with the real DI logger after host build.</summary>
    public void SetLogger(ILogger<ModuleRegistry> logger) => _logger = logger;

    // ── Read API ──────────────────────────────────────────────────────────────

    /// <summary>Read-only ordered list of registered modules.</summary>
    public IReadOnlyList<IModule> Modules => _modules.AsReadOnly();

    // ── Phase 1: DI Build ─────────────────────────────────────────────────────

    /// <summary>
    /// Scans <paramref name="assemblies"/> for concrete <see cref="IModule"/>
    /// implementations, topologically sorts them, calls
    /// <see cref="IModule.RegisterServices"/> on each, then seeds authorization
    /// policies from <see cref="IModule.GetPermissions"/>.
    ///
    /// Call this from <c>IServiceCollection.AddApiServices()</c> — BEFORE
    /// <c>WebApplication.Build()</c> is called.
    /// </summary>
    public void Discover(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        params Assembly[] assemblies)
    {
        var discovered = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.IsAssignableTo(typeof(IModule)))
            .Select(t =>
            {
                try   { return (IModule?)Activator.CreateInstance(t); }
                catch { return null; }
            })
            .OfType<IModule>()
            .ToDictionary(m => m.ModuleId);

        _logger.LogInformation(
            "[ModuleRegistry] Discovered {Count} module(s): {Ids}",
            discovered.Count,
            string.Join(", ", discovered.Keys));

        var sorted = TopologicalSort(discovered);

        foreach (var module in sorted)
        {
            _logger.LogInformation(
                "[ModuleRegistry] Registering '{Id}' v{Version} — {Name}",
                module.ModuleId, module.Version, module.DisplayName);

            module.RegisterServices(services, configuration, environment);
            _modules.Add(module);
        }

        // OWASP A01: seed one named policy per permission — centralized RBAC
        SeedAuthorizationPolicies(services);
    }

    // ── Phase 2: HTTP Pipeline ────────────────────────────────────────────────

    /// <summary>
    /// Calls <see cref="IModule.ConfigurePipeline"/> on every registered module.
    /// Call this from <c>WebApplication.UseApiPipeline()</c>.
    /// </summary>
    public void ConfigureAll(WebApplication app)
    {
        if (_modules.Count == 0)
        {
            _logger.LogWarning(
                "[ModuleRegistry] ConfigureAll called but no modules are registered. " +
                "Ensure Discover() was called during DI setup.");
            return;
        }

        foreach (var module in _modules)
        {
            _logger.LogDebug("[ModuleRegistry] Configuring pipeline for: {Id}", module.ModuleId);
            module.ConfigurePipeline(app);
        }
    }

    // ── Catalog Queries ───────────────────────────────────────────────────────

    /// <summary>All widgets declared across all modules.</summary>
    public IEnumerable<WidgetDescriptor> GetAllWidgets()
        => _modules.SelectMany(m => m.GetWidgets());

    /// <summary>All default templates declared across all modules.</summary>
    public IEnumerable<DefaultTemplateDescriptor> GetAllDefaultTemplates()
        => _modules.SelectMany(m => m.GetDefaultTemplates());

    /// <summary>All permissions declared across all modules (OWASP A01 catalog).</summary>
    public IEnumerable<PermissionDescriptor> GetAllPermissions()
        => _modules.SelectMany(m => m.GetPermissions());

    /// <summary>
    /// Navigation items filtered by <paramref name="context"/>
    /// (<see cref="UiContext.Admin"/> or <see cref="UiContext.Operation"/>).
    /// Items with <see cref="UiContext.Both"/> appear in both shells.
    /// </summary>
    public IEnumerable<NavigationItem> GetNavigationItems(UiContext context)
        => _modules
            .SelectMany(m => m.GetNavigationItems())
            .Where(n => n.Context == context || n.Context == UiContext.Both)
            .OrderBy(n => n.Order);

    // ── Private ───────────────────────────────────────────────────────────────

    private void SeedAuthorizationPolicies(IServiceCollection services)
    {
        var allPermissions = _modules
            .SelectMany(m => m.GetPermissions())
            .ToList();

        if (allPermissions.Count == 0) return;

        services.AddAuthorization(auth =>
        {
            foreach (var perm in allPermissions)
            {
                if (auth.GetPolicy(perm.Permission) is not null) continue;

                auth.AddPolicy(perm.Permission, policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireClaim("permission", perm.Permission));
            }
        });

        _logger.LogInformation(
            "[ModuleRegistry] Seeded {Count} RBAC authorization policies.",
            allPermissions.Count);
    }

    // ── Kahn's Algorithm — Topological Sort ───────────────────────────────────

    private static List<IModule> TopologicalSort(Dictionary<string, IModule> modules)
    {
        var inDegree = modules.Keys.ToDictionary(k => k, _ => 0);
        var adj      = modules.Keys.ToDictionary(k => k, _ => new List<string>());

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

        var queue  = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = new List<IModule>(modules.Count);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(modules[current]);

            foreach (var next in adj[current])
                if (--inDegree[next] == 0)
                    queue.Enqueue(next);
        }

        if (sorted.Count != modules.Count)
            throw new InvalidOperationException(
                "Circular module dependency detected. Check IModule.Dependencies declarations.");

        return sorted;
    }
}
