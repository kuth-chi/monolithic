using BusinessEntity = Monolithic.Api.Modules.Business.Domain.Business;

namespace Monolithic.Api.Modules.Identity.Domain;

/// <summary>
/// Links an <see cref="ApplicationUser"/> to a <see cref="BusinessEntity"/> they belong to (tenant membership).
/// Supports many-to-many: one user can belong to several businesses.
/// Business rule: exactly ONE <see cref="IsDefault"/> = true entry is allowed per user at any time.
/// </summary>
public sealed class UserBusiness
{
    public Guid Id { get; set; }

    /// <summary>FK → ApplicationUser.Id</summary>
    public Guid UserId { get; set; }

    /// <summary>FK → Business.Id</summary>
    public Guid BusinessId { get; set; }

    /// <summary>
    /// When true this is the business automatically selected on login.
    /// Enforced at service layer: clearing all others before setting this one.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>Soft-delete: exclude inactive memberships from login/switch flows.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When the membership was created.</summary>
    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // ── Navigation ───────────────────────────────────────────────────────
    public ApplicationUser User { get; set; } = null!;
    public BusinessEntity Business { get; set; } = null!;
}
