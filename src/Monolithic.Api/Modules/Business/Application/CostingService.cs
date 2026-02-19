using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Business.Application;

public sealed class CostingService(ApplicationDbContext context) : ICostingService
{
    // ── Setup ─────────────────────────────────────────────────────────────────
    public async Task<CostingSetupDto> UpsertSetupAsync(UpsertCostingSetupRequest request, CancellationToken ct = default)
    {
        var existing = await context.CostingSetups
            .FirstOrDefaultAsync(s => s.BusinessId == request.BusinessId && s.InventoryItemId == request.InventoryItemId, ct);

        if (existing is null)
        {
            existing = new CostingSetup
            {
                Id = Guid.NewGuid(),
                BusinessId = request.BusinessId,
                InventoryItemId = request.InventoryItemId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            context.CostingSetups.Add(existing);
        }

        existing.CostingMethod = request.CostingMethod;
        existing.StandardCost = request.StandardCost;
        existing.OverheadPercentage = request.OverheadPercentage;
        existing.LabourCostPerUnit = request.LabourCostPerUnit;
        existing.LandedCostPercentage = request.LandedCostPercentage;
        existing.IsActive = true;
        existing.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);

        return MapSetupToDto(existing, null);
    }

    public async Task<CostingSetupDto?> GetSetupAsync(Guid businessId, Guid? inventoryItemId = null, CancellationToken ct = default)
    {
        var setup = await context.CostingSetups.AsNoTracking()
            .Include(s => s.InventoryItem)
            .FirstOrDefaultAsync(s => s.BusinessId == businessId && s.InventoryItemId == inventoryItemId, ct);
        return setup is null ? null : MapSetupToDto(setup, setup.InventoryItem?.Name);
    }

    public async Task<IReadOnlyList<CostingSetupDto>> GetAllSetupsAsync(Guid businessId, CancellationToken ct = default)
    {
        var setups = await context.CostingSetups.AsNoTracking()
            .Include(s => s.InventoryItem)
            .Where(s => s.BusinessId == businessId && s.IsActive)
            .OrderBy(s => s.InventoryItemId == null ? 0 : 1) // Business-level first
            .ToListAsync(ct);
        return setups.Select(s => MapSetupToDto(s, s.InventoryItem?.Name)).ToList();
    }

    // ── Cost Ledger ───────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<CostLedgerEntryDto>> GetLedgerAsync(
        Guid businessId,
        Guid inventoryItemId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = context.CostLedgerEntries.AsNoTracking()
            .Include(e => e.InventoryItem)
            .Where(e => e.BusinessId == businessId && e.InventoryItemId == inventoryItemId);

        if (from.HasValue) query = query.Where(e => e.EntryDateUtc >= from.Value);
        if (to.HasValue) query = query.Where(e => e.EntryDateUtc <= to.Value);

        var entries = await query.OrderBy(e => e.EntryDateUtc).ToListAsync(ct);
        return entries.Select(e => new CostLedgerEntryDto
        {
            Id = e.Id,
            InventoryItemId = e.InventoryItemId,
            InventoryItemName = e.InventoryItem?.Name ?? string.Empty,
            EntryType = e.EntryType.ToString(),
            ReferenceNumber = e.ReferenceNumber,
            Quantity = e.Quantity,
            UnitCost = e.UnitCost,
            TotalCost = e.TotalCost,
            AverageUnitCostAfter = e.AverageUnitCostAfter,
            StockQuantityAfter = e.StockQuantityAfter,
            StockValueAfter = e.StockValueAfter,
            EntryDateUtc = e.EntryDateUtc,
            Notes = e.Notes
        }).ToList();
    }

    // ── Analysis ──────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<ItemCostAnalysisDto>> GetCostAnalysisAsync(
        Guid businessId,
        DateTimeOffset? cogsFrom = null,
        DateTimeOffset? cogsTo = null,
        CancellationToken ct = default)
    {
        var business = await context.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId, ct);

        // Get all inventory items with stock in this business
        var stockItems = await context.Stocks.AsNoTracking()
            .Include(s => s.InventoryItem)
            .Where(s => s.InventoryItem.BusinessId == businessId)
            .GroupBy(s => s.InventoryItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                TotalQty = g.Sum(s => s.QuantityOnHand),
                TotalValue = g.Sum(s => s.QuantityOnHand)   // Placeholder: value requires costing
            })
            .ToListAsync(ct);

        var itemIds = stockItems.Select(x => x.ItemId).ToList();

        // Get latest cost ledger entry per item for average cost
        var latestCostEntries = await context.CostLedgerEntries.AsNoTracking()
            .Where(e => e.BusinessId == businessId && itemIds.Contains(e.InventoryItemId))
            .GroupBy(e => e.InventoryItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                AvgCost = g.OrderByDescending(e => e.EntryDateUtc).First().AverageUnitCostAfter,
                StockValue = g.OrderByDescending(e => e.EntryDateUtc).First().StockValueAfter
            })
            .ToListAsync(ct);

        // COGS = sum of outbound cost ledger entries in period
        var cogsQuery = context.CostLedgerEntries.AsNoTracking()
            .Where(e => e.BusinessId == businessId && e.Quantity < 0 && itemIds.Contains(e.InventoryItemId));
        if (cogsFrom.HasValue) cogsQuery = cogsQuery.Where(e => e.EntryDateUtc >= cogsFrom.Value);
        if (cogsTo.HasValue) cogsQuery = cogsQuery.Where(e => e.EntryDateUtc <= cogsTo.Value);
        var cogsPerItem = await cogsQuery
            .GroupBy(e => e.InventoryItemId)
            .Select(g => new { ItemId = g.Key, COGS = g.Sum(e => -e.TotalCost) })
            .ToDictionaryAsync(x => x.ItemId, x => x.COGS, ct);

        // Item setups and names
        var setups = await context.CostingSetups.AsNoTracking()
            .Where(s => s.BusinessId == businessId)
            .ToDictionaryAsync(s => s.InventoryItemId ?? Guid.Empty, ct);

        var items = await context.InventoryItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var businessSetup = setups.GetValueOrDefault(Guid.Empty);

        return stockItems.Select(s =>
        {
            var item = items.GetValueOrDefault(s.ItemId);
            var setup = setups.GetValueOrDefault(s.ItemId) ?? businessSetup;
            var costEntry = latestCostEntries.FirstOrDefault(e => e.ItemId == s.ItemId);
            var avgCost = costEntry?.AvgCost ?? setup?.StandardCost ?? 0;
            var stockValue = costEntry?.StockValue ?? avgCost * s.TotalQty;
            var effectiveCost = setup?.CostingMethod == Domain.CostingMethod.StandardCost
                ? setup.StandardCost
                : avgCost;

            return new ItemCostAnalysisDto
            {
                InventoryItemId = s.ItemId,
                Sku = item?.Sku ?? string.Empty,
                Name = item?.Name ?? string.Empty,
                CostingMethod = setup?.CostingMethod.ToString() ?? "WeightedAverage",
                CurrentAverageUnitCost = avgCost,
                StandardCost = setup?.StandardCost ?? 0,
                EffectiveUnitCost = effectiveCost,
                TotalStockQuantity = s.TotalQty,
                TotalStockValue = stockValue,
                TotalCogs = cogsPerItem.GetValueOrDefault(s.ItemId, 0),
                BaseCurrencyCode = business?.BaseCurrencyCode ?? "USD"
            };
        }).ToList();
    }

    public async Task<ItemCostAnalysisDto?> GetItemCostAnalysisAsync(
        Guid businessId,
        Guid inventoryItemId,
        DateTimeOffset? cogsFrom = null,
        DateTimeOffset? cogsTo = null,
        CancellationToken ct = default)
    {
        var all = await GetCostAnalysisAsync(businessId, cogsFrom, cogsTo, ct);
        return all.FirstOrDefault(x => x.InventoryItemId == inventoryItemId);
    }

    private static CostingSetupDto MapSetupToDto(CostingSetup s, string? itemName) =>
        new()
        {
            Id = s.Id,
            BusinessId = s.BusinessId,
            InventoryItemId = s.InventoryItemId,
            InventoryItemName = itemName ?? (s.InventoryItemId == null ? "(Business Default)" : string.Empty),
            CostingMethod = s.CostingMethod.ToString(),
            StandardCost = s.StandardCost,
            OverheadPercentage = s.OverheadPercentage,
            LabourCostPerUnit = s.LabourCostPerUnit,
            LandedCostPercentage = s.LandedCostPercentage,
            IsActive = s.IsActive
        };
}
