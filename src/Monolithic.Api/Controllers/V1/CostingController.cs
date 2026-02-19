using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Costing setup and analysis.
/// Supports FIFO, Weighted Average, Standard Cost, Specific Identification, LIFO costing methods.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/costing")]
public sealed class CostingController(ICostingService service) : ControllerBase
{
    // ── Setup ────────────────────────────────────────────────────────────────

    /// <summary>Get business-level costing setup (method, overhead %, labour, landed cost %).</summary>
    [HttpGet("setup")]
    [RequirePermission("costing:read")]
    public async Task<IActionResult> GetBusinessSetup(Guid businessId, CancellationToken ct)
        => Ok(await service.GetSetupAsync(businessId, null, ct));

    /// <summary>Get or create per-item costing setup override.</summary>
    [HttpGet("setup/items/{inventoryItemId:guid}")]
    [RequirePermission("costing:read")]
    public async Task<IActionResult> GetItemSetup(Guid businessId, Guid inventoryItemId, CancellationToken ct)
        => Ok(await service.GetSetupAsync(businessId, inventoryItemId, ct));

    /// <summary>List all costing setups for the business (business-level + per-item overrides).</summary>
    [HttpGet("setup/all")]
    [RequirePermission("costing:read")]
    public async Task<IActionResult> GetAllSetups(Guid businessId, CancellationToken ct)
        => Ok(await service.GetAllSetupsAsync(businessId, ct));

    /// <summary>Create or update the business-level costing default.</summary>
    [HttpPut("setup")]
    [RequirePermission("costing:write")]
    public async Task<IActionResult> UpsertBusinessSetup(
        Guid businessId,
        [FromBody] UpsertCostingSetupRequest request,
        CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var normalized = new UpsertCostingSetupRequest
        {
            BusinessId = businessId,
            InventoryItemId = null,
            CostingMethod = request.CostingMethod,
            StandardCost = request.StandardCost,
            OverheadPercentage = request.OverheadPercentage,
            LabourCostPerUnit = request.LabourCostPerUnit,
            LandedCostPercentage = request.LandedCostPercentage
        };
        return Ok(await service.UpsertSetupAsync(normalized, ct));
    }

    /// <summary>Create or update costing override for a specific inventory item.</summary>
    [HttpPut("setup/items/{inventoryItemId:guid}")]
    [RequirePermission("costing:write")]
    public async Task<IActionResult> UpsertItemSetup(
        Guid businessId,
        Guid inventoryItemId,
        [FromBody] UpsertCostingSetupRequest request,
        CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var normalized = new UpsertCostingSetupRequest
        {
            BusinessId = businessId,
            InventoryItemId = inventoryItemId,
            CostingMethod = request.CostingMethod,
            StandardCost = request.StandardCost,
            OverheadPercentage = request.OverheadPercentage,
            LabourCostPerUnit = request.LabourCostPerUnit,
            LandedCostPercentage = request.LandedCostPercentage
        };
        return Ok(await service.UpsertSetupAsync(normalized, ct));
    }

    // ── Cost Ledger ──────────────────────────────────────────────────────────

    /// <summary>
    /// Get cost ledger entries for an inventory item.
    /// Shows all cost movements (receipts, shipments, adjustments, COGS).
    /// Useful for FIFO layer tracing and costing analysis.
    /// </summary>
    [HttpGet("items/{inventoryItemId:guid}/ledger")]
    [RequirePermission("costing:read")]
    public async Task<IActionResult> GetLedger(
        Guid businessId,
        Guid inventoryItemId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
        => Ok(await service.GetLedgerAsync(businessId, inventoryItemId, from, to, ct));

    // ── Analysis ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Full costing analysis across all items in the business.
    /// Returns current stock value, total COGS, average unit costs.
    /// </summary>
    [HttpGet("analysis")]
    [RequirePermission("costing:read")]
    public async Task<IActionResult> GetAnalysis(
        Guid businessId,
        [FromQuery] DateTimeOffset? cogsFrom = null,
        [FromQuery] DateTimeOffset? cogsTo = null,
        CancellationToken ct = default)
        => Ok(await service.GetCostAnalysisAsync(businessId, cogsFrom, cogsTo, ct));

    /// <summary>
    /// Detailed costing analysis for a specific inventory item.
    /// Includes cost history, current stock layer values, running average cost.
    /// </summary>
    [HttpGet("items/{inventoryItemId:guid}/analysis")]
    [RequirePermission("costing:read")]
    public async Task<IActionResult> GetItemAnalysis(
        Guid businessId,
        Guid inventoryItemId,
        [FromQuery] DateTimeOffset? cogsFrom = null,
        [FromQuery] DateTimeOffset? cogsTo = null,
        CancellationToken ct = default)
        => Ok(await service.GetItemCostAnalysisAsync(businessId, inventoryItemId, cogsFrom, cogsTo, ct));
}
