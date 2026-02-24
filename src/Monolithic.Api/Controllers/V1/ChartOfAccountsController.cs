using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Chart of Accounts management. Supports hierarchy (parent/child accounts), seeding, and per-business COA.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/chart-of-accounts")]
[Authorize]
[ServiceFilter(typeof(ValidateBusinessAccessFilter))]
public sealed class ChartOfAccountsController(IChartOfAccountService coaService) : ControllerBase
{
    /// <summary>Returns the COA as a nested tree.</summary>
    [HttpGet("tree")]
    [RequirePermission("business:coa:read", "finance:read")]
    public async Task<IActionResult> GetTree(Guid businessId, CancellationToken ct)
        => Ok(await coaService.GetTreeAsync(businessId, ct));

    /// <summary>Returns the COA as a flat list (for dropdowns).</summary>
    [HttpGet]
    [RequirePermission("business:coa:read", "finance:read")]
    public async Task<IActionResult> GetFlat(Guid businessId, CancellationToken ct)
        => Ok(await coaService.GetFlatAsync(businessId, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("business:coa:read", "finance:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await coaService.GetByIdAsync(businessId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("business:coa:write", "finance:write")]
    public async Task<IActionResult> Create(Guid businessId, [FromBody] CreateChartOfAccountRequest request, CancellationToken ct)
    {
        if (request.BusinessId != businessId)
            return BadRequest("BusinessId mismatch.");
        var result = await coaService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("business:coa:write", "finance:write")]
    public async Task<IActionResult> Update(Guid businessId, Guid id, [FromBody] UpdateChartOfAccountRequest request, CancellationToken ct)
        => Ok(await coaService.UpdateAsync(businessId, id, request, ct));

    /// <summary>Seeds a standard chart of accounts for the business (1000-7999 numbered accounts).</summary>
    [HttpPost("seed")]
    [RequirePermission("business:coa:write", "finance:write")]
    public async Task<IActionResult> Seed(
        Guid businessId,
        [FromQuery] string baseCurrency = "USD",
        CancellationToken ct = default)
    {
        await coaService.SeedStandardCOAAsync(businessId, baseCurrency, ct);
        return Ok(new { message = "Standard chart of accounts seeded.", businessId });
    }
}
