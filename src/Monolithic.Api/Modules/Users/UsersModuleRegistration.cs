using Microsoft.AspNetCore.Authorization;
using Monolithic.Api.Modules.Identity.Application;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Users.Application;

namespace Monolithic.Api.Modules.Users;

public static class UsersModuleRegistration
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        // ── Core services ──────────────────────────────────────────────────────
        services.AddScoped<IUserService, IdentityBackedUserService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();

        // ── Self-data / ABAC ───────────────────────────────────────────────────
        // ISelfDataContext: service-layer ownership check (reads from HttpContext)
        services.AddHttpContextAccessor();
        services.AddScoped<ISelfDataContext, HttpSelfDataContext>();

        // SelfOwnershipAuthorizationHandler: resource-based authorization handler
        // Handles SelfDataRequirement<IOwned> — checks sub == OwnerId or elevated permission
        services.AddScoped<IAuthorizationHandler, SelfOwnershipAuthorizationHandler>();

        return services;
    }
}