namespace Monolithic.Api.Modules.Finance.Domain;

/// <summary>A single expense line within an expense report.</summary>
public class ExpenseItem
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid? ExpenseCategoryId { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal AmountBase { get; set; }

    public DateOnly ExpenseDate { get; set; }

    /// <summary>Optional receipt / attachment URL.</summary>
    public string? ReceiptUrl { get; set; }

    public bool IsBillable { get; set; }

    /// <summary>Customer to bill if this is a billable expense.</summary>
    public Guid? CustomerId { get; set; }

    public string Notes { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Expense Expense { get; set; } = null!;
}
