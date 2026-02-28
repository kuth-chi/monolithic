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

    /// <summary>
    /// When <c>true</c> this permission uses the <c>self</c> action convention
    /// (<c>{module}:{resource}:self</c>) and is evaluated via the
    /// <c>SelfOwnershipAuthorizationHandler</c> resource-based authorization pipeline
    /// rather than a simple claim presence check.
    ///
    /// Assigning this permission to a role means role members may access their
    /// OWN records; not other users' records.
    /// </summary>
    public bool IsSelfScoped { get; set; } = false;

    // navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = [];

    public ICollection<UserPermission> UserPermissions { get; set; } = [];
}
