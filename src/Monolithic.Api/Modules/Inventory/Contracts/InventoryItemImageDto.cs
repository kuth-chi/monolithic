namespace Monolithic.Api.Modules.Inventory.Contracts;

/// <summary>
/// Read model for an inventory item image.
/// Scope: VariantId == null = main image; VariantId != null = variant-specific image.
/// </summary>
public sealed record InventoryItemImageDto(
    Guid Id,
    Guid InventoryItemId,
    Guid? VariantId,
    string OriginalFileName,
    /// <summary>Publicly accessible URL resolved by IImageStorageService.</summary>
    string ImageUrl,
    string ContentType,
    long FileSizeBytes,
    string AltText,
    int DisplayOrder,
    bool IsMain,
    Guid? UploadedByUserId,
    DateTimeOffset UploadedAtUtc
);
