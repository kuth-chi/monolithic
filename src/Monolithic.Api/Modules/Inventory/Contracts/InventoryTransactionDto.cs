using Monolithic.Api.Modules.Inventory.Domain;

namespace Monolithic.Api.Modules.Inventory.Contracts;

/// <summary>
/// Read model for a single inventory transaction / movement.
/// </summary>
public sealed record InventoryTransactionDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string InventoryItemSku,
    Guid WarehouseLocationId,
    string WarehouseLocationCode,
    string WarehouseName,
    InventoryTransactionType TransactionType,
    decimal Quantity,
    string ReferenceNumber,
    string Notes,
    Guid? PerformedByUserId,
    DateTimeOffset CreatedAtUtc
);
