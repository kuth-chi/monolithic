namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Estimate Purchase Order (RFQ – Request for Quotation).
/// The first step in the purchasing process: business requests a price quote from a vendor.
/// 
/// Lifecycle: Draft → SentToVendor → VendorQuoteReceived → Approved → ConvertedToPo
/// </summary>
public class EstimatePurchaseOrder
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid VendorId { get; set; }

    /// <summary>RFQ reference number (unique within the business).</summary>
    public string RfqNumber { get; set; } = string.Empty;

    public EstimatePurchaseOrderStatus Status { get; set; } = EstimatePurchaseOrderStatus.Draft;

    public DateTimeOffset RequestDateUtc { get; set; }

    /// <summary>Date the quote was received from the vendor.</summary>
    public DateTimeOffset? QuoteReceivedDateUtc { get; set; }

    /// <summary>Expiry date of the vendor's quotation.</summary>
    public DateTimeOffset? QuoteExpiryDateUtc { get; set; }

    // ── Currency ──────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";

    public decimal ExchangeRate { get; set; } = 1m;

    // ── Amounts (quoted by vendor) ────────────────────────────────────────────
    public decimal SubTotal { get; set; }

    public DiscountType OrderDiscountType { get; set; } = DiscountType.None;

    public decimal OrderDiscountValue { get; set; }

    public decimal OrderDiscountAmount { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalAmountBase { get; set; }

    // ── Misc ──────────────────────────────────────────────────────────────────
    public string Notes { get; set; } = string.Empty;

    public string VendorQuoteReference { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public DateTimeOffset? ApprovedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    public virtual Vendor Vendor { get; set; } = null!;

    public virtual ICollection<EstimatePurchaseOrderItem> Items { get; set; } = [];

    /// <summary>Purchase orders that were created from this RFQ.</summary>
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
}
