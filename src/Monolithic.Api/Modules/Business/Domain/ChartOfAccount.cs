namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Chart of Accounts entry.
/// 
/// Standard COA numbering convention:
///   1xxx  Assets
///   2xxx  Liabilities  
///   3xxx  Equity
///   4xxx–5xxx  Revenue
///   6xxx–9xxx  Expenses
/// 
/// Supports hierarchy (parent/child accounts) so you can have:
///   1000 - Cash and Cash Equivalents  (parent)
///     1010 - Petty Cash               (child)
///     1020 - Bank Account - USD       (child)
///     1030 - Bank Account - KHR       (child)
/// </summary>
public class ChartOfAccount
{
    public Guid Id { get; set; }

    /// <summary>The business this account belongs to.</summary>
    public Guid BusinessId { get; set; }

    /// <summary>Account number, e.g. "1010", "2000".</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Account name, e.g. "Cash", "Accounts Payable".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional longer description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Top-level classification: Asset, Liability, Equity, Revenue, Expense.</summary>
    public AccountType AccountType { get; set; }

    /// <summary>Sub-classification for reporting granularity.</summary>
    public AccountCategory AccountCategory { get; set; }

    /// <summary>Parent account ID for hierarchical COA (null = root-level account).</summary>
    public Guid? ParentAccountId { get; set; }

    /// <summary>
    /// Whether this account is a header/group (cannot post transactions directly)
    /// or a detail/posting account.
    /// </summary>
    public bool IsHeaderAccount { get; set; } = false;

    /// <summary>ISO currency code this account is denominated in (null = business base currency).</summary>
    public string? CurrencyCode { get; set; }

    /// <summary>Whether this account can still be used for new transactions.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Whether this is a system-managed account that should not be deleted.</summary>
    public bool IsSystem { get; set; } = false;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // Navigation
    public virtual Business Business { get; set; } = null!;

    public virtual ChartOfAccount? ParentAccount { get; set; }

    public virtual ICollection<ChartOfAccount> ChildAccounts { get; set; } = [];

    public virtual ICollection<PurchaseOrder> LinkedPurchaseOrders { get; set; } = [];

    public virtual ICollection<VendorBill> LinkedVendorBills { get; set; } = [];
}
