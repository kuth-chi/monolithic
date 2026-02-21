namespace Monolithic.Api.Common.SoftDelete;

/// <summary>
/// Marks an entity as soft-deletable.
/// Instead of physically removing the row the system sets <see cref="IsDeleted"/> = true
/// and records when / by whom the deletion was requested.
/// A background purge service then hard-deletes rows whose <see cref="DeletedAtUtc"/>
/// is older than the tenant's configured <c>SoftDeleteRetentionDays</c>.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>True when the record has been soft-deleted and is hidden from normal queries.</summary>
    bool IsDeleted { get; set; }

    /// <summary>UTC timestamp of the soft-deletion. Null while the record is live.</summary>
    DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>
    /// Id of the user who performed the soft-deletion.
    /// Null when the deletion was triggered by an automated process.
    /// </summary>
    Guid? DeletedByUserId { get; set; }
}
