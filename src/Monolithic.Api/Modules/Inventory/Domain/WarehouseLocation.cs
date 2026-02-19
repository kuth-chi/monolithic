namespace Monolithic.Api.Modules.Inventory.Domain;

/// <summary>
/// Represents a named physical location inside a Warehouse (e.g., Rack A1, Bin B3, Shelf 2).
/// Stock quantities for inventory items are tracked at this granularity.
/// </summary>
public class WarehouseLocation
{
    public Guid Id { get; set; }

    /// <summary>
    /// The warehouse this location belongs to.
    /// </summary>
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Short unique code within the warehouse (e.g., "A1", "BIN-003", "SHELF-2B").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name (e.g., "Aisle A Rack 1", "Cold Bin 3").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional zone/area grouping (e.g., "Cold", "Dry", "Hazmat").
    /// </summary>
    public string Zone { get; set; } = string.Empty;

    /// <summary>
    /// Optional description or notes about this location.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Navigation: parent warehouse.
    /// </summary>
    public virtual Warehouse Warehouse { get; set; } = null!;

    /// <summary>
    /// Navigation: stock records at this location.
    /// </summary>
    public virtual ICollection<Stock> Stocks { get; set; } = [];
}
