namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Inventory costing method for an item or business.
/// </summary>
public enum CostingMethod
{
    /// <summary>First In First Out — cost of oldest stock used first.</summary>
    FIFO = 1,

    /// <summary>Weighted Average Cost — running average of all purchase costs.</summary>
    WeightedAverage = 2,

    /// <summary>Standard Cost — fixed predetermined cost; variances tracked separately.</summary>
    StandardCost = 3,

    /// <summary>Specific Identification — actual cost of each specific unit tracked.</summary>
    SpecificIdentification = 4,

    /// <summary>Last In First Out (not GAAP in many countries, but supported).</summary>
    LIFO = 5
}
