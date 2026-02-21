namespace Monolithic.Api.Modules.Sales.Domain;

/// <summary>
/// Customer Quotation (RFQ reply / proposal).
/// Flow: Draft → Sent → Accepted | Rejected | Expired → (Accepted) → SalesOrder (Converted).
/// </summary>
public class Quotation
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }

    /// <summary>Auto-generated reference, e.g. QUO-2026-00001.</summary>
    public string QuotationNumber { get; set; } = string.Empty;

    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    public DateOnly QuotationDate { get; set; }

    /// <summary>Date after which the quotation is no longer valid.</summary>
    public DateOnly ExpiryDate { get; set; }

    // ── Currency ──────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;

    // ── Amounts ───────────────────────────────────────────────────────────────
    public decimal SubTotal { get; set; }
    public SalesDiscountType OrderDiscountType { get; set; } = SalesDiscountType.None;
    public decimal OrderDiscountValue { get; set; }
    public decimal OrderDiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string Notes { get; set; } = string.Empty;
    public string TermsAndConditions { get; set; } = string.Empty;

    /// <summary>SalesOrder created when the quotation is accepted and converted.</summary>
    public Guid? ConvertedToSalesOrderId { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual ICollection<QuotationItem> Items { get; set; } = [];
}
