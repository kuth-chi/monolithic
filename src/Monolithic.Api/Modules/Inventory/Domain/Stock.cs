namespace Monolithic.Api.Modules.Inventory.Domain;

/// <summary>
/// Tracks the quantity of an InventoryItem at a specific WarehouseLocation.
/// This is the source-of-truth for "how much of X is at location Y".
/// InventoryTransactions drive changes to these quantities.
/// </summary>
public class Stock
{
    public Guid Id { get; set; }

    /// <summary>
    /// The inventory item (product/SKU).
    /// </summary>
    public Guid InventoryItemId { get; set; }

    /// <summary>
    /// The physical location inside a warehouse.
    /// </summary>
    public Guid WarehouseLocationId { get; set; }

    /// <summary>
    /// Current quantity on hand at this location. Updated on every transaction.
    /// </summary>
    public decimal QuantityOnHand { get; set; }

    /// <summary>
    /// Quantity currently reserved (e.g., allocated to an outgoing order).
    /// </summary>
    public decimal QuantityReserved { get; set; }

    /// <summary>
    /// Available quantity = QuantityOnHand - QuantityReserved.
    /// </summary>
    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Navigation: the inventory item this stock belongs to.
    /// </summary>
    public virtual InventoryItem InventoryItem { get; set; } = null!;

    /// <summary>
    /// Navigation: the warehouse location where this stock is held.
    /// </summary>
    public virtual WarehouseLocation WarehouseLocation { get; set; } = null!;
}
