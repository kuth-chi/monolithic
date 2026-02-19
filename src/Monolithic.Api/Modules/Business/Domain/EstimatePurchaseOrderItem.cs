namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Line item in an Estimate Purchase Order (RFQ).
/// </summary>
public class EstimatePurchaseOrderItem
{
    public Guid Id { get; set; }

    public Guid EstimatePurchaseOrderId { get; set; }

    public Guid InventoryItemId { get; set; }

    public Guid? InventoryItemVariantId { get; set; }

    /// <summary>Quantity requested.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Estimated or quoted unit price.</summary>
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

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual EstimatePurchaseOrder EstimatePurchaseOrder { get; set; } = null!;

    public virtual Inventory.Domain.InventoryItem InventoryItem { get; set; } = null!;

    public virtual Inventory.Domain.InventoryItemVariant? InventoryItemVariant { get; set; }
}
