using Microsoft.AspNetCore.Authorization;

namespace Monolithic.Api.Modules.Identity.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string[] Permissions { get; }

    public PermissionRequirement(params string[] permissions)
    {
        Permissions = permissions;
    }
}
