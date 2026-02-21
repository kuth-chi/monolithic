using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Monolithic.Api.Modules.Platform.Core.Abstractions;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Core plug-and-play contract for every feature module in the system.
///
/// Usage:
///   1. Implement <see cref="IModule"/> in your module's registration class.
///   2. The <see cref="ModuleRegistry"/> auto-discovers all implementations at
///      startup via reflection and registers them in dependency order.
///
/// Design principles:
///   – DRY: shared infrastructure (DB, cache, auth) lives in Platform, modules
///     declare only their own services.
///   – OCP: add new features by adding new modules — zero changes to Platform.
///   – Dependency inversion: modules depend on Platform abstractions, never
///     on each other directly (cross-module calls go through service interfaces).
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public interface IModule
{
    /// <summary>
    /// Stable, lowercase, hyphen-separated identifier. Used for dependency
    /// resolution, feature-flag keys, and audit logs.
    /// Example: "inventory", "purchase-orders".
    /// </summary>
    string ModuleId { get; }

    /// <summary>Human-readable display name shown in admin UI.</summary>
    string DisplayName { get; }

    /// <summary>SemVer string. Logged at startup and exposed through the
    /// <c>/api/v1/platform/modules</c> endpoint.</summary>
    string Version { get; }

    /// <summary>
    /// Other module IDs that must be registered before this one.
    /// The registry performs a topological sort to honour this order.
    /// </summary>
    IEnumerable<string> Dependencies => [];

    /// <summary>
    /// Register all services, repositories, and options required by this module.
    /// Called once per application lifetime during startup.
    /// </summary>
    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Configure HTTP pipeline middleware, endpoints, or static files for this
    /// module. Called after all modules have registered their services.
    /// </summary>
    void ConfigurePipeline(WebApplication app) { }

    /// <summary>
    /// Declare widgets that this module contributes to dashboards.
    /// Returned <see cref="WidgetDescriptor"/> entries are persisted in the
    /// widget catalog so users can configure their personal layouts.
    /// </summary>
    IEnumerable<WidgetDescriptor> GetWidgets() => [];

    /// <summary>
    /// Default notification / report / PDF / SMS templates provided by the module.
    /// Seeded once at first startup; users can override at the business level.
    /// </summary>
    IEnumerable<DefaultTemplateDescriptor> GetDefaultTemplates() => [];
}
