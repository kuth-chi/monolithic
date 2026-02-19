namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// A line item in a Vendor Bill, tracing back to a PO item.
/// </summary>
public class VendorBillItem
{
    public Guid Id { get; set; }

    public Guid VendorBillId { get; set; }

    /// <summary>The PO item this bill line covers (nullable for ad-hoc bill lines).</summary>
    public Guid? PurchaseOrderItemId { get; set; }

    public Guid InventoryItemId { get; set; }

    public Guid? InventoryItemVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DiscountType DiscountType { get; set; } = DiscountType.None;

    public decimal DiscountValue { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal LineTotalBeforeDiscount { get; set; }

    public decimal LineTotalAfterDiscount { get; set; }

    public decimal LineTotal { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual VendorBill VendorBill { get; set; } = null!;

    public virtual PurchaseOrderItem? PurchaseOrderItem { get; set; }

    public virtual Inventory.Domain.InventoryItem InventoryItem { get; set; } = null!;

    public virtual Inventory.Domain.InventoryItemVariant? InventoryItemVariant { get; set; }
}
