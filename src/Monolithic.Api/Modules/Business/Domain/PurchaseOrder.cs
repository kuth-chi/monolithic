namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Represents a Purchase Order placed with a vendor.
/// 
/// Lifecycle: Draft → PendingApproval → Approved → PartiallyReceived → FullyReceived → Billed → Closed
/// May be created from an EstimatePurchaseOrder (RFQ/Quotation).
/// </summary>
public class PurchaseOrder
{
    public Guid Id { get; set; }

    /// <summary>The business placing the purchase order.</summary>
    public Guid BusinessId { get; set; }

    /// <summary>The vendor supplying the items.</summary>
    public Guid VendorId { get; set; }

    /// <summary>
    /// Optional link to the originating Estimate PO / RFQ.
    /// </summary>
    public Guid? EstimatePurchaseOrderId { get; set; }

    /// <summary>
    /// Optional link to the Chart of Accounts entry for this PO
    /// (typically the Inventory or Expense account being debited).
    /// </summary>
    public Guid? ChartOfAccountId { get; set; }

    /// <summary>Purchase order reference number (unique within the business).</summary>
    public string PoNumber { get; set; } = string.Empty;

    /// <summary>Typed status with full lifecycle support.</summary>
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    // ── On-Hold ──────────────────────────────────────────────────────────────
    /// <summary>Whether the PO is paused pending resolution (e.g., price dispute).</summary>
    public bool IsOnHold { get; set; } = false;

    /// <summary>Reason the PO was placed on hold.</summary>
    public string HoldReason { get; set; } = string.Empty;

    public DateTimeOffset? HoldStartedAtUtc { get; set; }

    // ── Dates ─────────────────────────────────────────────────────────────────
    public DateTimeOffset OrderDateUtc { get; set; }

    public DateTimeOffset? ExpectedDeliveryDateUtc { get; set; }

    public DateTimeOffset? ReceivedDateUtc { get; set; }

    // ── Currency ──────────────────────────────────────────────────────────────
    /// <summary>ISO currency code for this PO (e.g. "USD", "KHR").</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Exchange rate to the business base currency at the time of order.
    /// 1 CurrencyCode = ExchangeRate BaseCurrency.
    /// </summary>
    public decimal ExchangeRate { get; set; } = 1m;

    // ── Amounts ───────────────────────────────────────────────────────────────
    /// <summary>Sum of all line items before any discounts or taxes.</summary>
    public decimal SubTotal { get; set; }

    /// <summary>How the order-level discount is expressed.</summary>
    public DiscountType OrderDiscountType { get; set; } = DiscountType.None;

    /// <summary>Order-level discount value (amount or percentage).</summary>
    public decimal OrderDiscountValue { get; set; }

    /// <summary>Computed order-level discount in currency.</summary>
    public decimal OrderDiscountAmount { get; set; }

    /// <summary>Shipping / freight fee for the entire order.</summary>
    public decimal ShippingFee { get; set; }

    /// <summary>Total tax amount on all line items.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Grand total = SubTotal - OrderDiscountAmount + ShippingFee + TaxAmount.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Total amount converted to business base currency.</summary>
    public decimal TotalAmountBase { get; set; }

    // ── Misc ──────────────────────────────────────────────────────────────────
    public string Notes { get; set; } = string.Empty;

    public string InternalNotes { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public DateTimeOffset? ApprovedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    public virtual Vendor Vendor { get; set; } = null!;

    public virtual EstimatePurchaseOrder? EstimatePurchaseOrder { get; set; }

    public virtual ChartOfAccount? ChartOfAccount { get; set; }

    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = [];

    public virtual ICollection<VendorBill> VendorBills { get; set; } = [];
}
