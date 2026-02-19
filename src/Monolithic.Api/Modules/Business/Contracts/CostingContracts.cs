using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Business.Contracts;

// ── Costing Contracts ─────────────────────────────────────────────────────────

public sealed class CostingSetupDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public Guid? InventoryItemId { get; init; }
    public string InventoryItemName { get; init; } = string.Empty;
    public string CostingMethod { get; init; } = string.Empty;
    public decimal StandardCost { get; init; }
    public decimal OverheadPercentage { get; init; }
    public decimal LabourCostPerUnit { get; init; }
    public decimal LandedCostPercentage { get; init; }
    public bool IsActive { get; init; }
}

public sealed class UpsertCostingSetupRequest
{
    public Guid BusinessId { get; init; }
    public Guid? InventoryItemId { get; init; }
    public Domain.CostingMethod CostingMethod { get; init; } = Domain.CostingMethod.WeightedAverage;
    public decimal StandardCost { get; init; }
    public decimal OverheadPercentage { get; init; }
    public decimal LabourCostPerUnit { get; init; }
    public decimal LandedCostPercentage { get; init; }
}

// ── Cost Analysis DTOs ────────────────────────────────────────────────────────

/// <summary>
/// Summary of cost per item for analysis / COGS reporting.
/// </summary>
public sealed class ItemCostAnalysisDto
{
    public Guid InventoryItemId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string CostingMethod { get; init; } = string.Empty;
    public decimal CurrentAverageUnitCost { get; init; }
    public decimal StandardCost { get; init; }
    public decimal EffectiveUnitCost { get; init; }
    public decimal TotalStockQuantity { get; init; }
    public decimal TotalStockValue { get; init; }
    public decimal TotalCogs { get; init; }           // COGS for selected period
    public string BaseCurrencyCode { get; init; } = string.Empty;
}

/// <summary>
/// A single cost ledger entry line.
/// </summary>
public sealed class CostLedgerEntryDto
{
    public Guid Id { get; init; }
    public Guid InventoryItemId { get; init; }
    public string InventoryItemName { get; init; } = string.Empty;
    public string EntryType { get; init; } = string.Empty;
    public string ReferenceNumber { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public decimal TotalCost { get; init; }
    public decimal AverageUnitCostAfter { get; init; }
    public decimal StockQuantityAfter { get; init; }
    public decimal StockValueAfter { get; init; }
    public DateTimeOffset EntryDateUtc { get; init; }
    public string Notes { get; init; } = string.Empty;
}
