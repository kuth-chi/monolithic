namespace Monolithic.Api.Modules.Inventory.Domain;

/// <summary>
/// Represents a physical warehouse or storage facility owned by a Business.
/// A warehouse contains one or more WarehouseLocations (bins/racks/shelves).
/// </summary>
public class Warehouse
{
    public Guid Id { get; set; }

    /// <summary>
    /// The business that owns this warehouse.
    /// </summary>
    public Guid BusinessId { get; set; }

    /// <summary>
    /// Unique warehouse code within the business (e.g., "WH-01").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name (e.g., "Main Warehouse", "Cold Storage").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description or notes about the warehouse.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Physical address of the warehouse.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string StateProvince { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this is the default warehouse for the business.
    /// Only one warehouse per business may be the default.
    /// </summary>
    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Navigation: all physical locations inside this warehouse.
    /// </summary>
    public virtual ICollection<WarehouseLocation> Locations { get; set; } = [];
}
