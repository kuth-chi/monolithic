namespace Monolithic.Api.Modules.Inventory.Contracts;

/// <summary>
/// Read model for an InventoryItem (product/SKU catalogue entry).
/// </summary>
public sealed record InventoryItemDto(
    Guid Id,
    Guid BusinessId,
    string Sku,
    string Name,
    string Description,
    string Unit,
    decimal ReorderLevel,
    decimal ReorderQuantity,
    decimal CostPrice,
    decimal SellingPrice,
    bool IsActive,
    /// <summary>Aggregated total QuantityOnHand across all warehouse locations.</summary>
    decimal TotalQuantityOnHand,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc
);
