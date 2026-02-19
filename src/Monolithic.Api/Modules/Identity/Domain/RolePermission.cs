namespace Monolithic.Api.Modules.Identity.Domain;

public sealed class RolePermission
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    // navigation
    public ApplicationRole? Role { get; set; }

    public Permission? Permission { get; set; }
}
