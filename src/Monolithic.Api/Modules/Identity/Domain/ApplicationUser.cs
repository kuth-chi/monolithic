using Microsoft.AspNetCore.Identity;
using Monolithic.Api.Common.SoftDelete;

namespace Monolithic.Api.Modules.Identity.Domain;

public class ApplicationUser : IdentityUser<Guid>, ISoftDeletable
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginUtc { get; set; }

    // ── ISoftDeletable ────────────────────────────────────────────────────────
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
