namespace Monolithic.Api.Modules.Identity.Domain;

public sealed class UserPermission
{
    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    public bool Granted { get; set; } = true;

    // navigation
    public ApplicationUser? User { get; set; }

    public Permission? Permission { get; set; }
}
