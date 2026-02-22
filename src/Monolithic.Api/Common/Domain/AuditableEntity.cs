namespace Monolithic.Api.Common.Domain;

/// <summary>
/// Adds standard audit-trail columns to every persisted entity.
/// Inherit instead of <see cref="EntityBase"/> for entities that require audit tracking.
///
/// Column mapping (applied via EF Core conventions):
///   created_at_utc   — set once on INSERT, never updated
///   modified_at_utc  — set on every UPDATE, null until first modification
///   created_by_user_id  — optional user who created the record
///   modified_by_user_id — optional user who last modified the record
/// </summary>
public abstract class AuditableEntity : EntityBase
{
    /// <summary>UTC timestamp when the record was first persisted.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>UTC timestamp of the most recent update. Null until first modification.</summary>
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    /// <summary>User who created this record. Null for system-generated records.</summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>User who last modified this record. Null until first modification.</summary>
    public Guid? ModifiedByUserId { get; set; }
}
