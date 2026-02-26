namespace Monolithic.Api.Modules.Identity.Domain;

public sealed class Permission
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public string FeatureName { get; set; } = string.Empty;

    public string ActionName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = [];

    public ICollection<UserPermission> UserPermissions { get; set; } = [];
}
