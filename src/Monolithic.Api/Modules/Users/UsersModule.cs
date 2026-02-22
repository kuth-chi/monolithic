using Monolithic.Api.Modules.Platform.Core;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Users;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Users module — user profiles, role assignments, and access control.
///
/// Dependencies: identity (Identity establishes the user store)
///
/// UI Shell : Admin (user list, role assignment, access control)
///            Both  (user profile — available in both shells)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class UsersModule : ModuleBase
{
    public override string              ModuleId     => "users";
    public override string              DisplayName  => "Users & Access Control";
    public override string              Version      => "1.0.0";
    public override string              Description  => "User profiles, role assignments, and permission-based access control.";
    public override string              Icon         => "users";
    public override IEnumerable<string> Dependencies => ["identity"];

    public override void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        => services.AddUsersModule();

    public override IEnumerable<NavigationItem> GetNavigationItems()
    {
        // ── Admin UI ──────────────────────────────────────────────────────────
        yield return Nav("admin-root",  "Users & Access",  "/admin/users",          UiContext.Admin, icon: "users",          order: 30, isGroup: true);
        yield return Nav("list",        "All Users",       "/admin/users",          UiContext.Admin, icon: "user-group",     order: 31, parentKey: "users.admin-root", requiredPermissions: ["users:profiles:read"]);
        yield return Nav("roles",       "Roles",           "/admin/users/roles",    UiContext.Admin, icon: "shield-check",   order: 32, parentKey: "users.admin-root", requiredPermissions: ["users:roles:read"]);
        yield return Nav("permissions", "Permissions",     "/admin/users/permissions",UiContext.Admin, icon: "key",          order: 33, parentKey: "users.admin-root", requiredPermissions: ["users:roles:admin"]);

        // ── Both shells — user profile self-service ───────────────────────────
        yield return Nav("profile",     "My Profile",      "/profile",              UiContext.Both, icon: "user-circle", order: 999);
    }

    public override IEnumerable<PermissionDescriptor> GetPermissions()
    {
        yield return Perm("profiles", "read",   "View User Profiles",         defaultRoles: ["admin", "manager"]);
        yield return Perm("profiles", "write",  "Edit User Profiles",         defaultRoles: ["admin"]);
        yield return Perm("profiles", "delete", "Deactivate Users",           defaultRoles: ["admin"],  isSensitive: true);
        yield return Perm("roles",    "read",   "View Roles",                 defaultRoles: ["admin", "manager"]);
        yield return Perm("roles",    "admin",  "Manage Roles & Permissions", defaultRoles: ["admin"],  isSensitive: true);
    }
}
