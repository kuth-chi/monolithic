using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Modules.Business.Application;

public interface ICostingService
{
    // ── Setup ─────────────────────────────────────────────────────────────────
    Task<CostingSetupDto> UpsertSetupAsync(UpsertCostingSetupRequest request, CancellationToken ct = default);
    Task<CostingSetupDto?> GetSetupAsync(Guid businessId, Guid? inventoryItemId = null, CancellationToken ct = default);
    Task<IReadOnlyList<CostingSetupDto>> GetAllSetupsAsync(Guid businessId, CancellationToken ct = default);

    // ── Cost Ledger ───────────────────────────────────────────────────────────
    Task<IReadOnlyList<CostLedgerEntryDto>> GetLedgerAsync(
        Guid businessId,
        Guid inventoryItemId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken ct = default);

    // ── Analysis ──────────────────────────────────────────────────────────────
    Task<IReadOnlyList<ItemCostAnalysisDto>> GetCostAnalysisAsync(
        Guid businessId,
        DateTimeOffset? cogsFrom = null,
        DateTimeOffset? cogsTo = null,
        CancellationToken ct = default);

    Task<ItemCostAnalysisDto?> GetItemCostAnalysisAsync(
        Guid businessId,
        Guid inventoryItemId,
        DateTimeOffset? cogsFrom = null,
        DateTimeOffset? cogsTo = null,
        CancellationToken ct = default);
}
