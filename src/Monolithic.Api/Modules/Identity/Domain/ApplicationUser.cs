using Microsoft.AspNetCore.Identity;
using Monolithic.Api.Common.SoftDelete;

namespace Monolithic.Api.Modules.Identity.Domain;

public class ApplicationUser : IdentityUser<Guid>, ISoftDeletable
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginUtc { get; set; }

    // ── Account Suspension (set by Fake License Detective on 3 strikes) ───────
    /// <summary>UTC timestamp when the account was suspended. Null = not suspended.</summary>
    public DateTimeOffset? SuspendedAtUtc { get; set; }

    /// <summary>Human-readable reason recorded at suspension time.</summary>
    public string? SuspendedReason { get; set; }

    // ── ISoftDeletable ────────────────────────────────────────────────────────
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
