namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// A single debit or credit line within a <see cref="JournalEntry"/>.
///
/// Rules:
///  - <see cref="DebitAmount"/> XOR <see cref="CreditAmount"/> must be non-zero for each line
///    (never both set on the same line).
///  - Line is permanently immutable once the parent entry is posted.
///  - Supports up to 999+ lines per entry (no artificial cap enforced in domain).
/// </summary>
public class JournalEntryLine
{
    public Guid Id { get; set; }

    public Guid JournalEntryId { get; set; }

    /// <summary>GL account being debited or credited.</summary>
    public Guid AccountId { get; set; }

    /// <summary>1-based line sequence for display ordering and cycle references.</summary>
    public int LineNumber { get; set; }

    /// <summary>Debit amount in the entry's functional currency (0 if this is a credit line).</summary>
    public decimal DebitAmount { get; set; }

    /// <summary>Credit amount in the entry's functional currency (0 if this is a debit line).</summary>
    public decimal CreditAmount { get; set; }

    /// <summary>Debit amount converted to business base currency.</summary>
    public decimal DebitAmountBase { get; set; }

    /// <summary>Credit amount converted to business base currency.</summary>
    public decimal CreditAmountBase { get; set; }

    /// <summary>Optional dimension: cost center / department code for management reporting.</summary>
    public string? CostCenter { get; set; }

    /// <summary>Optional dimension: project / job code for project-based accounting.</summary>
    public string? ProjectCode { get; set; }

    /// <summary>Per-line narrative (supplements the entry description).</summary>
    public string? LineDescription { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────
    public virtual JournalEntry JournalEntry { get; set; } = null!;

    public virtual ChartOfAccount Account { get; set; } = null!;
}
