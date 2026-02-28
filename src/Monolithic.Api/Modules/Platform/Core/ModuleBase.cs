using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Platform.Core;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Convenient DRY base class for all feature modules.
///
/// Extend this instead of implementing <see cref="IModule"/> directly to get:
///   • Fluent builder helpers for navigation items and permissions
///   • Consistent permission key generation ("moduleId:resource:action")
///   • Virtual lifecycle methods (override only what you need)
///   • Compile-time enforcement that ModuleId / DisplayName / Version are set
///
/// Example:
/// <code>
/// public sealed class InventoryModule : ModuleBase
/// {
///     public override string ModuleId    => "inventory";
///     public override string DisplayName => "Inventory";
///     public override string Version     => "1.0.0";
///     public override string Description => "Stock items, warehouses and costing.";
///     public override string Icon        => "cube";
///
///     public override void RegisterServices(IServiceCollection s, IConfiguration c)
///     {
///         s.AddScoped&lt;IInventoryService, InventoryService&gt;();
///     }
/// }
/// </code>
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public abstract class ModuleBase : IModule
{
    // ── Identity (must override) ──────────────────────────────────────────────
    public abstract string ModuleId    { get; }
    public abstract string DisplayName { get; }
    public abstract string Version     { get; }

    // ── Optional metadata ─────────────────────────────────────────────────────
    public virtual string? Description => null;
    public virtual string? Icon        => null;

    // ── Dependencies ──────────────────────────────────────────────────────────
    public virtual IEnumerable<string> Dependencies => [];

    // ── Services (must override) ──────────────────────────────────────────────
    public abstract void RegisterServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment);

    // ── Pipeline (optional) ───────────────────────────────────────────────────
    public virtual void ConfigurePipeline(WebApplication app) { }

    // ── UI — Navigation ───────────────────────────────────────────────────────
    public virtual IEnumerable<NavigationItem> GetNavigationItems() => [];

    // ── UI — Widgets ──────────────────────────────────────────────────────────
    public virtual IEnumerable<WidgetDescriptor> GetWidgets() => [];

    // ── Templates ─────────────────────────────────────────────────────────────
    public virtual IEnumerable<DefaultTemplateDescriptor> GetDefaultTemplates() => [];

    // ── Database ──────────────────────────────────────────────────────────────
    public virtual DatabaseDescriptor? GetDatabaseDescriptor() => null;

    // ── Security — Permissions ────────────────────────────────────────────────
    public virtual IEnumerable<PermissionDescriptor> GetPermissions() => [];

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    public virtual Task OnFirstRunAsync(IServiceProvider services, CancellationToken ct = default)
        => Task.CompletedTask;

    public virtual Task OnEnabledAsync(IServiceProvider services, CancellationToken ct = default)
        => Task.CompletedTask;

    public virtual Task OnDisabledAsync(IServiceProvider services, CancellationToken ct = default)
        => Task.CompletedTask;

    // ── Builder Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="NavigationItem"/> scoped to this module.
    /// <paramref name="key"/> is automatically prefixed with <see cref="ModuleId"/>.
    /// </summary>
    protected NavigationItem Nav(
        string       key,
        string       label,
        string       route,
        UiContext    context,
        string?      icon                 = null,
        string?      parentKey            = null,
        int          order                = 0,
        string[]?    requiredPermissions  = null,
        bool         isGroup              = false,
        string?      badge                = null)
        => new(
            Key:                 $"{ModuleId}.{key}",
            Label:               label,
            Route:               route,
            Context:             context,
            ModuleId:            ModuleId,
            Icon:                icon,
            ParentKey:           parentKey,
            Order:               order,
            RequiredPermissions: requiredPermissions,
            IsGroup:             isGroup,
            Badge:               badge);

    /// <summary>
    /// Builds a <see cref="PermissionDescriptor"/> scoped to this module.
    /// <paramref name="resource"/> and <paramref name="action"/> are combined
    /// as "{ModuleId}:{resource}:{action}".
    /// </summary>
    protected PermissionDescriptor Perm(
        string    resource,
        string    action,
        string    displayName,
        string?   description  = null,
        string[]? defaultRoles = null,
        bool      isSensitive  = false)
        => new(
            Permission:   $"{ModuleId}:{resource}:{action}",
            DisplayName:  displayName,
            ModuleId:     ModuleId,
            Description:  description,
            DefaultRoles: defaultRoles,
            IsSensitive:  isSensitive);

    /// <summary>
    /// Builds a <b>self-data</b> <see cref="PermissionDescriptor"/> scoped to this module.
    ///
    /// The permission key is always <c>"{ModuleId}:{resource}:self"</c>.
    /// This permission uses resource-based authorization via
    /// <c>SelfOwnershipAuthorizationHandler</c>: the role holder may
    /// access only the records they own.
    ///
    /// Example output key: <c>"users:profiles:self"</c>
    /// </summary>
    protected PermissionDescriptor PermSelf(
        string    resource,
        string    displayName,
        string?   description  = null,
        string[]? defaultRoles = null)
        => new(
            Permission:   $"{ModuleId}:{resource}:self",
            DisplayName:  displayName,
            ModuleId:     ModuleId,
            Description:  description,
            DefaultRoles: defaultRoles,
            IsSensitive:  false,
            IsSelfData:   true);
}
