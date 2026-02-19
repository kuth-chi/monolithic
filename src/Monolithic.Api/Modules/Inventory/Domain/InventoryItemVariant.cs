namespace Monolithic.Api.Modules.Inventory.Domain;

/// <summary>
/// A variant of an InventoryItem representing a distinct combination of attributes
/// (e.g., "Red / XL", "Blue / SM"). Each variant may have its own images.
/// The parent InventoryItem is the base product; variants refine it.
/// </summary>
public class InventoryItemVariant
{
    public Guid Id { get; set; }

    /// <summary>
    /// The parent inventory item (product/SKU catalogue entry).
    /// </summary>
    public Guid InventoryItemId { get; set; }

    /// <summary>
    /// Human-readable variant name (e.g., "Red / XL", "Matte Black").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional SKU suffix appended to the parent SKU to form a unique variant SKU.
    /// Example: parent SKU "SHOE-001" + suffix "-RED-XL" = "SHOE-001-RED-XL".
    /// Leave empty to use the parent SKU alone.
    /// </summary>
    public string SkuSuffix { get; set; } = string.Empty;

    /// <summary>
    /// Signed price adjustment relative to the parent item's selling price.
    /// Positive = premium, negative = discount.
    /// </summary>
    public decimal PriceAdjustment { get; set; }

    /// <summary>
    /// Display sort order among sibling variants.
    /// </summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Navigation: parent item.
    /// </summary>
    public virtual InventoryItem InventoryItem { get; set; } = null!;

    /// <summary>
    /// Navigation: attribute key-value pairs that define this variant
    /// (e.g., Color=Red, Size=XL).
    /// </summary>
    public virtual ICollection<InventoryItemVariantAttribute> Attributes { get; set; } = [];

    /// <summary>
    /// Navigation: images specific to this variant.
    /// </summary>
    public virtual ICollection<InventoryItemImage> Images { get; set; } = [];
}
