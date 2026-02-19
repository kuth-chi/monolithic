namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Immutable audit log row for every significant action taken on a <see cref="JournalEntry"/>.
/// Captures who did what and when — satisfying the acceptance criterion for full audit trail.
/// </summary>
public class JournalEntryAuditLog
{
    public Guid Id { get; set; }

    public Guid JournalEntryId { get; set; }

    /// <summary>The user who performed the action.</summary>
    public Guid UserId { get; set; }

    /// <summary>Display name snapshot at time of action (denormalised for historical accuracy).</summary>
    public string UserDisplayName { get; set; } = string.Empty;

    public JournalEntryAuditAction Action { get; set; }

    /// <summary>UTC timestamp of the action.</summary>
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Optional free-text note attached to the action (e.g. reversal reason).</summary>
    public string? Notes { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────
    public virtual JournalEntry JournalEntry { get; set; } = null!;
}
