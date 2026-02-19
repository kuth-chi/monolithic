namespace Monolithic.Api.Modules.Inventory.Contracts;

/// <summary>
/// Read model for a Stock record (item × location × quantity).
/// </summary>
public sealed record StockDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemSku,
    string InventoryItemName,
    Guid WarehouseLocationId,
    string WarehouseLocationCode,
    string WarehouseLocationName,
    Guid WarehouseId,
    string WarehouseName,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal QuantityAvailable,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc
);
