using Monolithic.Api.Common.Domain;

namespace Monolithic.Api.Modules.Inventory.Domain;

/// <summary>
/// Represents an inventory item / product SKU (the catalogue entry).
/// Actual stock quantities are tracked per warehouse location via <see cref="Stock"/>.
/// Inherits <see cref="AuditableEntity"/> for Id, CreatedAtUtc, ModifiedAtUtc,
/// CreatedByUserId and ModifiedByUserId.
/// </summary>
public class InventoryItem : AuditableEntity
{
    /// <summary>
    /// The business this inventory item belongs to.
    /// </summary>
    public Guid BusinessId { get; set; }

    /// <summary>
    /// SKU (Stock Keeping Unit) — unique identifier within the business.
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Product / item name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the item.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Unit of measurement (e.g., "pieces", "kg", "liters").
    /// </summary>
    public string Unit { get; set; } = "pieces";

    /// <summary>
    /// Reorder point — trigger a purchase order when total stock falls below this level.
    /// </summary>
    public decimal ReorderLevel { get; set; }

    /// <summary>
    /// Suggested quantity to reorder when stock hits the reorder level.
    /// </summary>
    public decimal ReorderQuantity { get; set; }

    /// <summary>
    /// Default cost price per unit (can be overridden per PO line item).
    /// </summary>
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Default selling / retail price per unit.
    /// </summary>
    public decimal SellingPrice { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation: per-location stock records for this item.
    /// Sum <see cref="Stock.QuantityOnHand"/> across all locations for total stock.
    /// </summary>
    public virtual ICollection<Stock> Stocks { get; set; } = [];

    /// <summary>
    /// Navigation: variants of this item (e.g., Red/XL, Blue/SM).
    /// An item with no variants is treated as a single-presentation product.
    /// </summary>
    public virtual ICollection<InventoryItemVariant> Variants { get; set; } = [];

    /// <summary>
    /// Navigation: images where VariantId == null (main / cover images for all variants).
    /// Variant-specific images are accessed via each Variant's Images collection.
    /// </summary>
    public virtual ICollection<InventoryItemImage> Images { get; set; } = [];

    /// <summary>
    /// Navigation: all inventory movements (in / out / adjustment / transfer) for this item.
    /// </summary>
    public virtual ICollection<InventoryTransaction> Transactions { get; set; } = [];

    /// <summary>
    /// Navigation: purchase order line items referencing this SKU.
    /// </summary>
    public virtual ICollection<Business.Domain.PurchaseOrderItem> PurchaseOrderItems { get; set; } = [];
}
