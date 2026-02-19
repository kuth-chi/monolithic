namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// General Ledger Journal Entry header.
///
/// Design rules:
///  - Must be balanced before posting (Σ Debits = Σ Credits, enforced at service layer).
///  - Once <see cref="JournalEntryStatus.Posted"/>, header and lines are immutable.
///  - Reversals create a wholly new entry whose <see cref="ReversalOfEntryId"/> points back.
///  - Supports up to the database row limit of lines (spec requires ≥ 999).
///  - Multi-currency: each line carries its own currency amount; base-currency amount
///    is always computed and stored for single-currency GL reports.
/// </summary>
public class JournalEntry
{
    public Guid Id { get; set; }

    /// <summary>Business this entry belongs to.</summary>
    public Guid BusinessId { get; set; }

    /// <summary>Human-readable reference, e.g. "JE-2026-00001".</summary>
    public string EntryNumber { get; set; } = string.Empty;

    /// <summary>Accounting period this entry falls into (YYYY-MM, e.g. "2026-02").</summary>
    public string FiscalPeriod { get; set; } = string.Empty;

    /// <summary>Effective transaction date (the date that matters for GL reporting).</summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>Narrative / memo describing the purpose of the entry.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Current lifecycle status.</summary>
    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;

    /// <summary>Originating subsystem.</summary>
    public JournalEntrySourceType SourceType { get; set; } = JournalEntrySourceType.Manual;

    /// <summary>
    /// Free-form reference to the source document (e.g. vendor bill ID, purchase order number).
    /// Supports drill-down from GL to source.
    /// </summary>
    public string? SourceDocumentReference { get; set; }

    /// <summary>Optional link to a typed source document for AP-originated entries.</summary>
    public Guid? SourceDocumentId { get; set; }

    /// <summary>
    /// When this entry reverses a previously posted entry,
    /// this points to the original entry being reversed.
    /// </summary>
    public Guid? ReversalOfEntryId { get; set; }

    /// <summary>
    /// When this posted entry has been reversed,
    /// this points to the reversal entry that was created.
    /// </summary>
    public Guid? ReversedByEntryId { get; set; }

    /// <summary>ISO currency code used for all lines of this entry (functional currency).</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Exchange rate to business base currency at time of transaction (1.0 if same currency).</summary>
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>Sum of all debit lines (base currency). Populated on post for quick validation.</summary>
    public decimal TotalDebits { get; set; }

    /// <summary>Sum of all credit lines (base currency). Populated on post for quick validation.</summary>
    public decimal TotalCredits { get; set; }

    // ── Audit ───────────────────────────────────────────────────────────────
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid? PostedByUserId { get; set; }
    public DateTimeOffset? PostedAtUtc { get; set; }

    public Guid? ReversedByUserId { get; set; }
    public DateTimeOffset? ReversedAtUtc { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    public virtual ICollection<JournalEntryLine> Lines { get; set; } = [];

    public virtual ICollection<JournalEntryAuditLog> AuditLogs { get; set; } = [];

    /// <summary>Navigation to the original entry that this entry reverses (set only on Reversal entries).</summary>
    public virtual JournalEntry? ReversalOfEntry { get; set; }
}
