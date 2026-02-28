using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monolithic.Api.Modules.Platform.Core;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Infrastructure.Data;

namespace Monolithic.Api.Modules.Platform;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Platform Foundation module — the OS kernel of the business management system.
///
/// Provides shared infrastructure that all other modules consume:
///   • ModuleRegistry        – plug-and-play auto-discovery engine
///   • ITenantContext        – multi-tenant scoping per HTTP request
///   • IThemeService         – design-token theming engine
///   • IUserPreferenceService– dashboard / widget layout per user
///   • IFeatureFlagService   – per-business / per-user feature toggles
///   • ITemplateService      – Scriban template engine (Email / PDF / SMS / Push)
///   • INotificationService  – multi-channel notification dispatch
///
/// Dependencies: identity (requires DbContext and user context)
///
/// UI Shell : Admin (themes, templates, feature flags, notifications, module catalog)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class PlatformModule : ModuleBase
{
    public override string              ModuleId     => "platform";
    public override string              DisplayName  => "Platform Foundation";
    public override string              Version      => "1.0.0";
    public override string              Description  => "OS-level infrastructure: theming, templating, feature flags, notifications, tenant context, and module catalog.";
    public override string              Icon         => "cog-8-tooth";
    public override IEnumerable<string> Dependencies => ["identity"];

    public override void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        => services.AddPlatformModule(configuration, environment);

    public override DatabaseDescriptor? GetDatabaseDescriptor() =>
        new(ModuleId,
            ConnectionStringKey: "Infrastructure:Databases:Platform",
            DbContextType:       typeof(PlatformDbContext),
            DisplayName:         "Platform Foundation DB");

    public override IEnumerable<NavigationItem> GetNavigationItems()
    {
        // ── Admin UI ──────────────────────────────────────────────────────────
        yield return Nav("admin-root",    "Platform",           "/admin/platform",                   UiContext.Admin, icon: "cog-8-tooth",         order: 5, isGroup: true);
        yield return Nav("modules",       "Module Catalog",     "/admin/platform/modules",           UiContext.Admin, icon: "puzzle-piece",         order: 6,  parentKey: "platform.admin-root", requiredPermissions: ["platform:modules:read"]);
        yield return Nav("themes",        "Themes",             "/admin/platform/themes",            UiContext.Admin, icon: "swatch",               order: 7,  parentKey: "platform.admin-root", requiredPermissions: ["platform:themes:write"]);
        yield return Nav("templates",     "Templates",          "/admin/platform/templates",         UiContext.Admin, icon: "document-text",        order: 8,  parentKey: "platform.admin-root", requiredPermissions: ["platform:templates:write"]);
        yield return Nav("feature-flags", "Feature Flags",      "/admin/platform/feature-flags",     UiContext.Admin, icon: "beaker",               order: 9,  parentKey: "platform.admin-root", requiredPermissions: ["platform:feature-flags:write"]);
        yield return Nav("notifications", "Notifications",      "/admin/platform/notifications",     UiContext.Admin, icon: "bell",                 order: 10, parentKey: "platform.admin-root", requiredPermissions: ["platform:notifications:write"]);
        yield return Nav("preferences",   "User Preferences",   "/admin/platform/user-preferences",  UiContext.Admin, icon: "adjustments-horizontal",order: 11, parentKey: "platform.admin-root", requiredPermissions: ["platform:preferences:read"]);
        yield return Nav("system-update",  "System Update",      "/admin/platform/system-update",     UiContext.Admin, icon: "arrow-path",              order: 12, parentKey: "platform.admin-root", requiredPermissions: ["platform:docker:update"]);

        // ── Operation UI — notifications visible to all users ─────────────────
        yield return Nav("notif-op",   "Notifications",         "/notifications",                    UiContext.Operation, icon: "bell", order: 998);
    }

    public override IEnumerable<PermissionDescriptor> GetPermissions()
    {
        yield return Perm("modules",       "read",  "View Module Catalog",          defaultRoles: ["admin"]);
        yield return Perm("modules",       "admin", "Enable/Disable Modules",       defaultRoles: ["admin"], isSensitive: true);
        yield return Perm("themes",        "read",  "View Themes",                  defaultRoles: ["admin", "manager"]);
        yield return Perm("themes",        "write", "Manage Themes",                defaultRoles: ["admin"]);
        yield return Perm("templates",     "read",  "View Templates",               defaultRoles: ["admin", "manager"]);
        yield return Perm("templates",     "write", "Manage Templates",             defaultRoles: ["admin"]);
        yield return Perm("feature-flags", "read",  "View Feature Flags",           defaultRoles: ["admin"]);
        yield return Perm("feature-flags", "write", "Toggle Feature Flags",         defaultRoles: ["admin"], isSensitive: true);
        yield return Perm("notifications", "read",  "View Notification Settings",   defaultRoles: ["admin"]);
        yield return Perm("notifications", "write", "Manage Notification Settings", defaultRoles: ["admin"], isSensitive: true);
        yield return Perm("preferences",   "read",  "View User Preferences",        defaultRoles: ["admin", "manager"]);
        yield return Perm("info",          "read",  "View Platform Info / Manifest",defaultRoles: ["admin", "manager"]);
        yield return Perm("docker",        "update","Trigger Docker Image Updates",  defaultRoles: ["admin"], isSensitive: true);
    }
}
