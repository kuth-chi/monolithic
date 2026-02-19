namespace Monolithic.Api.Modules.Inventory.Domain;

/// <summary>
/// A single attribute key-value pair belonging to an InventoryItemVariant.
/// Example: AttributeName="Color", AttributeValue="Red".
/// Using a separate table (vs JSON) keeps attributes queryable and filterable.
/// </summary>
public class InventoryItemVariantAttribute
{
    public Guid Id { get; set; }

    /// <summary>
    /// The variant this attribute belongs to.
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// Attribute name, e.g., "Color", "Size", "Material".
    /// </summary>
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// Attribute value, e.g., "Red", "XL", "Cotton".
    /// </summary>
    public string AttributeValue { get; set; } = string.Empty;

    /// <summary>
    /// Navigation: parent variant.
    /// </summary>
    public virtual InventoryItemVariant Variant { get; set; } = null!;
}
