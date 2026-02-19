namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// A line item within a Purchase Order.
/// Supports per-line discount (amount or percentage) and tax rate.
/// </summary>
public class PurchaseOrderItem
{
    public Guid Id { get; set; }

    public Guid PurchaseOrderId { get; set; }

    public Guid InventoryItemId { get; set; }

    /// <summary>Optional link to a specific variant being ordered.</summary>
    public Guid? InventoryItemVariantId { get; set; }

    /// <summary>Quantity ordered.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Agreed unit price in the PO currency.</summary>
    public decimal UnitPrice { get; set; }

    // ── Per-line Discount ────────────────────────────────────────────────────
    public DiscountType DiscountType { get; set; } = DiscountType.None;

    /// <summary>Discount value (amount or percentage).</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Computed discount amount in currency.</summary>
    public decimal DiscountAmount { get; set; }

    // ── Tax ───────────────────────────────────────────────────────────────────
    /// <summary>Tax rate for this line (e.g. 0.10 for 10%).</summary>
    public decimal TaxRate { get; set; }

    /// <summary>Computed tax amount for this line.</summary>
    public decimal TaxAmount { get; set; }

    // ── Totals ────────────────────────────────────────────────────────────────
    /// <summary>Quantity * UnitPrice (before discount and tax).</summary>
    public decimal LineTotalBeforeDiscount { get; set; }

    /// <summary>LineTotalBeforeDiscount - DiscountAmount.</summary>
    public decimal LineTotalAfterDiscount { get; set; }

    /// <summary>LineTotalAfterDiscount + TaxAmount.</summary>
    public decimal LineTotal { get; set; }

    // ── Receipt Tracking ──────────────────────────────────────────────────────
    public decimal QuantityReceived { get; set; }

    public decimal QuantityBilled { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    public virtual Inventory.Domain.InventoryItem InventoryItem { get; set; } = null!;

    public virtual Inventory.Domain.InventoryItemVariant? InventoryItemVariant { get; set; }

    public virtual ICollection<VendorBillItem> BillItems { get; set; } = [];
}
