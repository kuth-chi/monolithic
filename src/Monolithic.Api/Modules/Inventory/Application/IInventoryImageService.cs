using Monolithic.Api.Modules.Inventory.Contracts;

namespace Monolithic.Api.Modules.Inventory.Application;

public interface IInventoryImageService
{
    // ── Variants ───────────────────────────────────────────────────────────

    Task<IReadOnlyCollection<InventoryItemVariantDto>> GetVariantsAsync(Guid inventoryItemId, CancellationToken cancellationToken = default);

    Task<InventoryItemVariantDto?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default);

    Task<InventoryItemVariantDto> CreateVariantAsync(Guid inventoryItemId, CreateInventoryItemVariantRequest request, CancellationToken cancellationToken = default);

    Task<InventoryItemVariantDto?> UpdateVariantAsync(Guid variantId, UpdateInventoryItemVariantRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteVariantAsync(Guid variantId, CancellationToken cancellationToken = default);

    // ── Images ─────────────────────────────────────────────────────────────

    /// <summary>Returns all images for an item (main + all variant images).</summary>
    Task<IReadOnlyCollection<InventoryItemImageDto>> GetImagesAsync(Guid inventoryItemId, Guid? variantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload an image for an item.
    /// Pass variantId to scope the image to a specific variant; omit for a main/all-variants image.
    /// </summary>
    Task<InventoryItemImageDto> UploadImageAsync(
        Guid inventoryItemId,
        IFormFile file,
        Guid? variantId,
        string altText,
        int displayOrder,
        bool setAsMain,
        Guid? uploadedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Update metadata only (alt text, display order, IsMain).</summary>
    Task<InventoryItemImageDto?> UpdateImageMetaAsync(Guid imageId, string altText, int displayOrder, bool isMain, CancellationToken cancellationToken = default);

    /// <summary>Delete an image record and its backing file.</summary>
    Task<bool> DeleteImageAsync(Guid imageId, CancellationToken cancellationToken = default);

    /// <summary>Reorder images for an item (or a specific variant). Provide ordered list of image IDs.</summary>
    Task ReorderImagesAsync(IReadOnlyList<Guid> orderedImageIds, CancellationToken cancellationToken = default);
}
