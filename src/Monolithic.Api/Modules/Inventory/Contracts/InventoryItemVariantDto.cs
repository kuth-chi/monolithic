namespace Monolithic.Api.Modules.Inventory.Contracts;

public sealed record InventoryItemVariantDto(
    Guid Id,
    Guid InventoryItemId,
    string Name,
    string SkuSuffix,
    decimal PriceAdjustment,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyCollection<InventoryItemVariantAttributeDto> Attributes,
    IReadOnlyCollection<InventoryItemImageDto> Images,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc
);
