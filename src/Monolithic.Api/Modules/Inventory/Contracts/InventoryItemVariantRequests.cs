using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Inventory.Contracts;

public sealed record UpsertVariantAttributeRequest(
    [Required, MaxLength(100)] string AttributeName,
    [Required, MaxLength(200)] string AttributeValue
);

public sealed record CreateInventoryItemVariantRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(100)] string SkuSuffix,
    decimal PriceAdjustment,
    int DisplayOrder,
    IReadOnlyCollection<UpsertVariantAttributeRequest> Attributes
);

public sealed record UpdateInventoryItemVariantRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(100)] string SkuSuffix,
    decimal PriceAdjustment,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyCollection<UpsertVariantAttributeRequest> Attributes
);
