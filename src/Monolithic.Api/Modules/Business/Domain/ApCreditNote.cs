namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// AP Credit Note — issued when a vendor refunds money or accepts a goods return.
/// Can be applied to outstanding bills (reducing AmountDue) or kept as a credit balance.
///
/// Types:
///   GoodsReturn  — physical return of goods to vendor (triggers inventory deduction).
///   PriceAdjustment — vendor issues debit/credit for price error.
///   Refund       — vendor sends money back (bank remittance).
///   Overpayment  — excess payment that vendor acknowledges as credit.
/// </summary>
public class ApCreditNote
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid VendorId { get; set; }

    /// <summary>Original bill this credit note relates to (nullable for standalone credits).</summary>
    public Guid? OriginalVendorBillId { get; set; }

    public ApCreditNoteType Type { get; set; } = ApCreditNoteType.PriceAdjustment;

    public ApCreditNoteStatus Status { get; set; } = ApCreditNoteStatus.Draft;

    /// <summary>Auto-generated reference number, e.g. "CN-20260218-0001".</summary>
    public string CreditNoteNumber { get; set; } = string.Empty;

    /// <summary>Vendor's own credit note / RMA reference.</summary>
    public string VendorReference { get; set; } = string.Empty;

    public DateOnly IssueDate { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>Credit amount in the credit note currency.</summary>
    public decimal CreditAmount { get; set; }

    /// <summary>Credit amount in business base currency.</summary>
    public decimal CreditAmountBase { get; set; }

    /// <summary>Amount already applied to bills. CreditAmount - AmountApplied = remaining balance.</summary>
    public decimal AmountApplied { get; set; } = 0m;

    /// <summary>Remaining unapplied credit balance.</summary>
    public decimal AmountRemaining { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public DateTimeOffset? ApprovedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    public virtual Vendor Vendor { get; set; } = null!;

    public virtual VendorBill? OriginalVendorBill { get; set; }

    public virtual ICollection<ApCreditNoteApplication> Applications { get; set; } = [];
}

public enum ApCreditNoteType
{
    GoodsReturn = 0,      // Physical return of goods
    PriceAdjustment = 1,  // Vendor-issued price correction
    Refund = 2,           // Money returned by vendor
    Overpayment = 3       // Excess payment acknowledged as credit
}

public enum ApCreditNoteStatus
{
    Draft = 0,
    Confirmed = 1,  // Credit confirmed, available for application
    Applied = 2,    // Fully applied to bills
    Cancelled = 3
}
