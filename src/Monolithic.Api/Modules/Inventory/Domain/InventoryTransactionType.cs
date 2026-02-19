namespace Monolithic.Api.Modules.Inventory.Domain;

/// <summary>
/// The type of an inventory movement. Using an enum enforces valid values at compile time.
/// </summary>
public enum InventoryTransactionType
{
    /// <summary>
    /// Stock received at a location (e.g., from a Purchase Order).
    /// Increases QuantityOnHand.
    /// </summary>
    In,

    /// <summary>
    /// Stock dispatched from a location (e.g., for a Sales Order or consumption).
    /// Decreases QuantityOnHand.
    /// </summary>
    Out,

    /// <summary>
    /// Manual count correction or damage write-off.
    /// Can increase or decrease QuantityOnHand (sign of Quantity determines direction).
    /// </summary>
    Adjustment,

    /// <summary>
    /// Stock moved from one WarehouseLocation to another.
    /// Produces two linked transactions: one Out from source, one In at destination.
    /// </summary>
    Transfer
}
