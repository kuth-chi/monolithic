namespace Monolithic.Api.Modules.Inventory.Domain;

/// <summary>
/// Metadata record for an image attached to an InventoryItem or one of its Variants.
/// The actual file is stored via <see cref="IImageStorageService"/> and referenced by <see cref="StoragePath"/>.
///
/// Scoping rules:
///   VariantId == null  → Main / cover image that applies to the item and all its variants.
///   VariantId != null  → Image specific to that variant only.
/// </summary>
public class InventoryItemImage
{
    public Guid Id { get; set; }

    /// <summary>
    /// The inventory item this image belongs to.
    /// </summary>
    public Guid InventoryItemId { get; set; }

    /// <summary>
    /// When set, this image is scoped to a specific variant.
    /// When null, this image is a main / all-variants image.
    /// </summary>
    public Guid? VariantId { get; set; }

    /// <summary>
    /// Original file name as uploaded (e.g., "front-view.jpg").
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Relative path within the storage root (e.g., "inventory/{itemId}/abc123.jpg").
    /// Resolved to a full URL by IImageStorageService.GetPublicUrl().
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type (e.g., "image/jpeg", "image/png", "image/webp").
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Alt text for accessibility / SEO (e.g., "Red XL T-Shirt front view").
    /// </summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>
    /// Display order within the item or variant's image gallery. Lower = first.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// When true this is the primary/cover image for the item or variant.
    /// Only one image per (InventoryItemId, VariantId) pair should be primary at a time.
    /// Enforced at the service layer.
    /// </summary>
    public bool IsMain { get; set; }

    /// <summary>
    /// User who uploaded the image.
    /// </summary>
    public Guid? UploadedByUserId { get; set; }

    public DateTimeOffset UploadedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // ── Navigations ────────────────────────────────────────────────────────

    public virtual InventoryItem InventoryItem { get; set; } = null!;

    /// <summary>Null when this is a main/all-variants image.</summary>
    public virtual InventoryItemVariant? Variant { get; set; }
}
