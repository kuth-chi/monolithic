namespace Monolithic.Api.Modules.Finance.Domain;

/// <summary>
/// Expense Report — a claim submitted by an employee for reimbursement.
/// Flow: Draft → Submitted → Approved | Rejected → Paid | Cancelled.
/// </summary>
public class Expense
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }

    /// <summary>Employee / user who incurred the expense.</summary>
    public Guid SubmittedByUserId { get; set; }

    /// <summary>Auto-generated reference, e.g. EXP-2026-00001.</summary>
    public string ExpenseNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;

    public DateOnly ExpenseDate { get; set; }

    // ── Currency ──────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;

    // ── Amounts ───────────────────────────────────────────────────────────────
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountBase { get; set; }

    public string Notes { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;

    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public DateTimeOffset? PaidAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual ICollection<ExpenseItem> Items { get; set; } = [];
}
