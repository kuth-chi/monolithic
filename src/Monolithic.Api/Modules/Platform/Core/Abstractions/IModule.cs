using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Monolithic.Api.Modules.Platform.Core.Abstractions;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Core plug-and-play contract for every feature module.
///
/// The platform acts as an OS kernel; each <see cref="IModule"/> is an
/// application that installs itself at startup without touching Platform code.
///
/// ┌──────────────────────────────────────────────────────────────────────────┐
/// │  HOW TO CREATE A NEW MODULE                                              │
/// │  1. Create a class: public sealed class MyModule : ModuleBase { … }     │
/// │  2. Implement ModuleId, DisplayName, Version.                            │
/// │  3. Override RegisterServices() — DI registration only.                 │
/// │  4. Optionally override GetNavigationItems(), GetPermissions(),          │
/// │     GetWidgets(), GetDefaultTemplates(), ConfigurePipeline(),            │
/// │     OnFirstRunAsync().                                                   │
/// │  5. Done — ModuleRegistry auto-discovers it via reflection.              │
/// └──────────────────────────────────────────────────────────────────────────┘
///
/// Design principles enforced by this contract:
///   DRY  — shared infra (DB, cache, auth, tenant, theme) lives in Platform.
///   OCP  — add features by adding modules; Platform code never changes.
///   DIP  — modules depend on Platform abstractions, not on each other.
///   OWASP A01 — modules self-declare permissions; Platform seeds RBAC from them.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public interface IModule
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Stable, lowercase, hyphen-separated identifier. Used for dependency
    /// resolution, feature-flag keys, permission prefixes, and audit logs.
    /// Example: "inventory", "finance", "purchase-orders".
    /// </summary>
    string ModuleId { get; }

    /// <summary>Human-readable display name shown in the admin module catalog.</summary>
    string DisplayName { get; }

    /// <summary>
    /// SemVer string (e.g. "1.0.0"). Logged at startup and exposed via
    /// <c>GET /api/v1/platform/modules</c>.
    /// </summary>
    string Version { get; }

    /// <summary>Short markdown description displayed in the module catalog.</summary>
    string? Description => null;

    /// <summary>Heroicons v2 outline slug (e.g. "cube", "chart-bar"). Null = default icon.</summary>
    string? Icon => null;

    /// <summary>
    /// Other module IDs that must be registered before this one.
    /// <see cref="ModuleRegistry"/> performs a topological sort to honour this order.
    /// </summary>
    IEnumerable<string> Dependencies => [];

    // ── Service Registration ──────────────────────────────────────────────────

    /// <summary>
    /// Register all DI services, repositories, and options for this module.
    /// Called once per application lifetime during the DI build phase — BEFORE
    /// <c>WebApplication.Build()</c> is called.
    /// Do NOT resolve services here; only call services.AddXxx().
    /// </summary>
    void RegisterServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment);

    // ── Pipeline Configuration ────────────────────────────────────────────────

    /// <summary>
    /// Configure HTTP middleware, endpoints, or static-file paths for this module.
    /// Called once after all module services are registered and the
    /// <see cref="WebApplication"/> has been built.
    /// </summary>
    void ConfigurePipeline(WebApplication app) { }

    // ── UI — Navigation (Admin UI / Operation UI) ─────────────────────────────

    /// <summary>
    /// Navigation items this module contributes to the frontend sidebar.
    ///
    /// The frontend calls <c>GET /api/v1/platform/navigation?context=admin</c>
    /// (or <c>operation</c>) and builds its menu from the response — zero
    /// hardcoding on the frontend needed.
    ///
    /// Use <see cref="UiContext.Admin"/> for administrative/setup screens.
    /// Use <see cref="UiContext.Operation"/> for day-to-day operational screens.
    /// Use <see cref="UiContext.Both"/> for items visible in both shells.
    /// </summary>
    IEnumerable<NavigationItem> GetNavigationItems() => [];

    // ── UI — Dashboard Widgets ─────────────────────────────────────────────────

    /// <summary>
    /// Widgets this module contributes to the dashboard widget catalog.
    /// Seeded once at startup; users configure their personal layout via
    /// <see cref="IUserPreferenceService"/>.
    /// </summary>
    IEnumerable<WidgetDescriptor> GetWidgets() => [];

    // ── Templates ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Default Scriban templates (Email / SMS / PDF / Push) provided by this module.
    /// Seeded once on first startup; businesses can override at business scope.
    /// </summary>
    IEnumerable<DefaultTemplateDescriptor> GetDefaultTemplates() => [];

    // ── Database ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes the database this module needs.
    ///
    /// Return a <see cref="DatabaseDescriptor"/> to opt into isolated-database
    /// mode — Platform Foundation's <c>ModuleDatabaseInitializer</c> will auto-
    /// migrate this module's database at startup.
    ///
    /// Return <c>null</c> (default) to share the common <c>ApplicationDbContext</c>.
    /// </summary>
    DatabaseDescriptor? GetDatabaseDescriptor() => null;

    // ── Security — RBAC (OWASP A01) ───────────────────────────────────────────

    /// <summary>
    /// All permissions (capabilities) this module declares.
    ///
    /// At startup <see cref="ModuleRegistry"/> aggregates all permissions across
    /// all modules and seeds ASP.NET Core authorization policies from them — no
    /// manual policy registration required when adding a new module.
    ///
    /// Convention: "{moduleId}:{resource}:{action}"
    /// Actions: read | write | delete | approve | export | admin
    ///
    /// Example: "inventory:items:write", "finance:reports:export"
    /// </summary>
    IEnumerable<PermissionDescriptor> GetPermissions() => [];

    // ── Lifecycle Hooks ───────────────────────────────────────────────────────

    /// <summary>
    /// Runs once, the very first time this module is registered on a fresh
    /// installation (detected by absence of module record in the DB).
    /// Use to seed reference data, default settings, or migration data.
    /// Idempotent — safe to be called again if previous run failed mid-way.
    /// </summary>
    Task OnFirstRunAsync(IServiceProvider services, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <summary>
    /// Called whenever the business owner enables this module on their tenant
    /// (via the Feature Flags / Module management UI).
    /// </summary>
    Task OnEnabledAsync(IServiceProvider services, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <summary>
    /// Called whenever the business owner disables this module on their tenant.
    /// Use to clean up scheduled jobs or temporary state.
    /// </summary>
    Task OnDisabledAsync(IServiceProvider services, CancellationToken ct = default)
        => Task.CompletedTask;
}
