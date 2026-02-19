namespace Monolithic.Api.Modules.Inventory.Contracts;

public sealed record InventoryItemVariantAttributeDto(
    Guid Id,
    string AttributeName,
    string AttributeValue
);
