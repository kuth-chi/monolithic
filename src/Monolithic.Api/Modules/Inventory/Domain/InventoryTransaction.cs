namespace Monolithic.Api.Modules.Inventory.Domain;

/// <summary>
/// Represents a single inventory movement at a specific WarehouseLocation.
/// Every change to Stock.QuantityOnHand must be backed by an InventoryTransaction.
/// </summary>
public class InventoryTransaction
{
    public Guid Id { get; set; }

    /// <summary>
    /// The inventory item (product/SKU) that moved.
    /// </summary>
    public Guid InventoryItemId { get; set; }

    /// <summary>
    /// The specific warehouse location where the movement occurred.
    /// For Transfer transactions this is the source or destination location
    /// depending on the TransactionType (Out = source, In = destination).
    /// </summary>
    public Guid WarehouseLocationId { get; set; }

    /// <summary>
    /// Type of movement: In, Out, Adjustment, or Transfer.
    /// </summary>
    public InventoryTransactionType TransactionType { get; set; }

    /// <summary>
    /// Absolute quantity involved. For Adjustment, a negative value means reduction.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Optional reference number (e.g., PO number, Sales Order number, Transfer ID).
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Notes or reason for the transaction.
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// The user who performed or triggered this transaction.
    /// </summary>
    public Guid? PerformedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Navigation: the inventory item.
    /// </summary>
    public virtual InventoryItem InventoryItem { get; set; } = null!;

    /// <summary>
    /// Navigation: the warehouse location.
    /// </summary>
    public virtual WarehouseLocation WarehouseLocation { get; set; } = null!;
}

