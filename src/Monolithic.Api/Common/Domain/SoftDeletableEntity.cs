using Monolithic.Api.Common.SoftDelete;

namespace Monolithic.Api.Common.Domain;

/// <summary>
/// Combines <see cref="AuditableEntity"/> with soft-delete support.
/// Use for any entity that must record who deleted it and when,
/// and that should be excluded from normal queries while retaining data for audit.
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity, ISoftDeletable
{
    /// <inheritdoc/>
    public bool IsDeleted { get; set; } = false;

    /// <inheritdoc/>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <inheritdoc/>
    public Guid? DeletedByUserId { get; set; }
}
