namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Join entity: maps a user (owner) to a business they own.
/// A user with an appropriate <see cref="BusinessLicense"/> may own multiple businesses.
/// Uses soft-ownership so a business can be co-owned or transferred safely.
/// </summary>
public class BusinessOwnership
{
    public Guid Id { get; set; }

    /// <summary>The owning user (ApplicationUser.Id).</summary>
    public Guid OwnerId { get; set; }

    /// <summary>The business being owned.</summary>
    public Guid BusinessId { get; set; }

    /// <summary>The license under which this ownership was granted.</summary>
    public Guid LicenseId { get; set; }

    /// <summary>
    /// Whether this owner is the primary (super) owner.
    /// A business can have only one primary owner.
    /// </summary>
    public bool IsPrimaryOwner { get; set; } = true;

    /// <summary>When ownership was granted.</summary>
    public DateTimeOffset GrantedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When ownership was revoked (soft-remove).</summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    public bool IsActive => RevokedAtUtc is null;

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;
    public virtual BusinessLicense License { get; set; } = null!;
}
