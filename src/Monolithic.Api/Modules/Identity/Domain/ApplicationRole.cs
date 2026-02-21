using Microsoft.AspNetCore.Identity;
using Monolithic.Api.Common.SoftDelete;

namespace Monolithic.Api.Modules.Identity.Domain;

public sealed class ApplicationRole : IdentityRole<Guid>, ISoftDeletable
{
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c> this role is a built-in system role (e.g. Owner, System Admin).
    /// System roles cannot be deleted, renamed, or have their core permissions stripped.
    /// </summary>
    public bool IsSystemRole { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // ── ISoftDeletable ────────────────────────────────────────────────────────
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
