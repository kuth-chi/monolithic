namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Costing configuration for a business (or optionally per item).
/// Determines how inventory cost is calculated when selling or adjusting stock.
/// </summary>
public class CostingSetup
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>
    /// If set, this overrides the business-level setup for a specific item.
    /// Null = applies to the whole business.
    /// </summary>
    public Guid? InventoryItemId { get; set; }

    /// <summary>Selected costing method.</summary>
    public CostingMethod CostingMethod { get; set; } = CostingMethod.WeightedAverage;

    // ── Standard Cost fields (used when CostingMethod = StandardCost) ────────
    /// <summary>Standard cost per unit (in base currency).</summary>
    public decimal StandardCost { get; set; }

    /// <summary>
    /// Overhead percentage added on top of purchase price to arrive at full absorption cost.
    /// e.g. 0.15 = 15%.
    /// </summary>
    public decimal OverheadPercentage { get; set; }

    /// <summary>
    /// Direct labour cost per unit (in base currency).
    /// </summary>
    public decimal LabourCostPerUnit { get; set; }

    /// <summary>
    /// Landed cost percentage (freight, duties, insurance) added to purchase price.
    /// e.g. 0.05 = 5%.
    /// </summary>
    public decimal LandedCostPercentage { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    public virtual Inventory.Domain.InventoryItem? InventoryItem { get; set; }
}
