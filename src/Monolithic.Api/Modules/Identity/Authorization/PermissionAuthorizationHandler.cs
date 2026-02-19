using Microsoft.AspNetCore.Authorization;

namespace Monolithic.Api.Modules.Identity.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permission = context.User.FindFirst(c =>
            c.Type == "permission" && requirement.Permissions.Contains(c.Value));

        if (permission is not null)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
