using Monolithic.Api.Modules.Platform.Core;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Identity;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Identity module — authentication (JWT), ASP.NET Core Identity setup,
/// EF Core DbContext, roles, and authorization infrastructure.
///
/// This is the FOUNDATION module that all other modules transitively depend on
/// because it provides:
///   • The shared <c>ApplicationDbContext</c> (all domain entities in one DB)
///   • <c>ApplicationUser</c> / <c>ApplicationRole</c> identity stores
///   • JWT Bearer authentication middleware
///   • <c>IAuthorizationHandler</c> for permission-based access control (OWASP A01)
///
/// Dependencies: none (root module)
///
/// UI Shell : Admin (auth configuration, audit logs)
///            Both  (login/logout — handled by the frontend without nav items)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class IdentityModule : ModuleBase
{
    public override string              ModuleId     => "identity";
    public override string              DisplayName  => "Identity & Authentication";
    public override string              Version      => "1.0.0";
    public override string              Description  => "JWT authentication, ASP.NET Core Identity, EF Core DbContext, and RBAC authorization infrastructure.";
    public override string              Icon         => "shield-check";

    // Identity has no module dependencies — it IS the root
    public override IEnumerable<string> Dependencies => [];

    public override void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        => services.AddIdentityModule(configuration, environment);

    public override void ConfigurePipeline(WebApplication app)
        => app.UseIdentityModule();

    // ApplicationDbContext is initialised via EnsureCreatedAsync in SeedData,
    // not through the module migration pipeline. Returning null here prevents
    // ModuleDatabaseInitializer from calling MigrateAsync on it, which would
    // conflict with the EnsureCreated strategy and raise PendingModelChangesWarning.
    public override DatabaseDescriptor? GetDatabaseDescriptor() => null;

    public override IEnumerable<NavigationItem> GetNavigationItems()
    {
        // ── Admin UI ──────────────────────────────────────────────────────────
        yield return Nav("admin-root", "Identity",      "/admin/identity",             UiContext.Admin, icon: "shield-check",  order: 10, isGroup: true);
        yield return Nav("auth-config","Auth Config",   "/admin/identity/auth",        UiContext.Admin, icon: "cog-6-tooth",   order: 11, parentKey: "identity.admin-root", requiredPermissions: ["identity:config:admin"]);
        yield return Nav("audit-logs", "Audit Logs",    "/admin/identity/audit-logs",  UiContext.Admin, icon: "clipboard-document-list", order: 12, parentKey: "identity.admin-root", requiredPermissions: ["identity:audit:read"]);
    }

    public override IEnumerable<PermissionDescriptor> GetPermissions()
    {
        // These are the foundational permissions used by [RequirePermission()] on every controller
        yield return Perm("config",  "admin", "Manage Authentication Config", defaultRoles: ["admin"],            isSensitive: true);
        yield return Perm("audit",   "read",  "View Auth Audit Logs",         defaultRoles: ["admin"],            isSensitive: true);
        // Platform info — read access (currently used by PlatformInfoController)
        yield return Perm("info",    "read",  "Read Platform Information",    defaultRoles: ["admin", "manager"]);
    }
}
