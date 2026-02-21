using Microsoft.AspNetCore.Authorization;

namespace Monolithic.Api.Modules.Identity.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private const string FullAccessPermission = "*:full";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Super-admin / owner: *:full satisfies every permission requirement
        var hasFullAccess = context.User.HasClaim(c =>
            c.Type == "permission" && c.Value == FullAccessPermission);

        if (hasFullAccess)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var permission = context.User.FindFirst(c =>
            c.Type == "permission" && requirement.Permissions.Contains(c.Value));

        if (permission is not null)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
