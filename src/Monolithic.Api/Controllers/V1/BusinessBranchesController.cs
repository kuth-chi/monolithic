using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Common.Pagination;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Branch management within a business.
/// Enforces: ≥1 branch, exactly one HQ, branch count ≤ license quota.
///
/// All list endpoints are paginated, filterable, and sortable via query parameters.
/// Results are served from a two-level cache (L1: in-memory, L2: Redis) with
/// write-through eviction on mutations.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/branches")]
public sealed class BusinessBranchesController(
    IBusinessBranchService branchService,
    IBusinessLicenseService licenseService) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst("sub")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value,
            out var id) ? id : Guid.Empty;

    /// <summary>
    /// Returns a paginated, searchable, and sortable list of branches for the business.
    /// </summary>
    /// <remarks>
    /// <b>Query parameters:</b>
    /// <list type="bullet">
    ///   <item><c>page</c> — 1-based page number (default: 1)</item>
    ///   <item><c>size</c> — items per page, capped at 100 (default: 20)</item>
    ///   <item><c>sortBy</c> — field: name | code | city | country | sortorder | createdat</item>
    ///   <item><c>sortDesc</c> — true for descending order</item>
    ///   <item><c>search</c> — full-text search on name, code, city</item>
    ///   <item><c>isActive</c> — filter by active status (omit for all)</item>
    ///   <item><c>isHeadquarters</c> — filter HQ / non-HQ branches</item>
    ///   <item><c>country</c> — substring filter on country</item>
    ///   <item><c>city</c> — substring filter on city</item>
    /// </list>
    /// </remarks>
    [HttpGet]
    [RequirePermission("business:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] BranchQueryParameters query,
        CancellationToken ct)
    {
        var result = await branchService.GetByBusinessAsync(businessId, query, ct);
        return Ok(result.WithNavigationUrls(Request));
    }

    [HttpGet("{branchId:guid}")]
    [RequirePermission("business:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid branchId, CancellationToken ct)
    {
        var result = await branchService.GetByIdAsync(branchId, ct);
        return result is null || result.BusinessId != businessId ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("business:write")]
    public async Task<IActionResult> Create(Guid businessId, [FromBody] CreateBranchRequest request, CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        if (!await licenseService.CanCreateBranchAsync(CurrentUserId, businessId, ct))
            return StatusCode(403, "License quota exceeded: cannot add more branches.");

        var result = await branchService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { businessId, branchId = result.Id }, result);
    }

    [HttpPut("{branchId:guid}")]
    [RequirePermission("business:write")]
    public async Task<IActionResult> Update(Guid businessId, Guid branchId, [FromBody] UpdateBranchRequest request, CancellationToken ct)
        => Ok(await branchService.UpdateAsync(branchId, request, ct));

    /// <summary>
    /// Promotes a branch to Headquarters and demotes the current HQ atomically.
    /// </summary>
    [HttpPost("promote-hq")]
    [RequirePermission("business:admin")]
    public async Task<IActionResult> PromoteHq(Guid businessId, [FromBody] PromoteHeadquartersRequest request, CancellationToken ct)
    {
        await branchService.PromoteHeadquartersAsync(businessId, request.NewHqBranchId, ct);
        return NoContent();
    }

    [HttpDelete("{branchId:guid}")]
    [RequirePermission("business:write")]
    public async Task<IActionResult> Delete(Guid businessId, Guid branchId, CancellationToken ct)
    {
        await branchService.DeleteAsync(branchId, ct);
        return NoContent();
    }

    // ── Branch Employees ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated list of employees assigned to the branch.
    /// </summary>
    /// <remarks>
    /// <b>Query parameters:</b>
    /// <list type="bullet">
    ///   <item><c>page</c>, <c>size</c>, <c>sortBy</c>, <c>sortDesc</c> — standard pagination</item>
    ///   <item><c>isPrimary</c> — filter by primary/non-primary assignment</item>
    ///   <item><c>isActive</c> — true = active assignments only (default), false = released only</item>
    /// </list>
    /// </remarks>
    [HttpGet("{branchId:guid}/employees")]
    [RequirePermission("business:read")]
    public async Task<IActionResult> GetEmployees(
        Guid businessId,
        Guid branchId,
        [FromQuery] BranchEmployeeQueryParameters query,
        CancellationToken ct)
    {
        var result = await branchService.GetEmployeesAsync(branchId, query, ct);
        return Ok(result.WithNavigationUrls(Request));
    }

    [HttpPost("{branchId:guid}/employees")]
    [RequirePermission("business:write")]
    public async Task<IActionResult> AssignEmployee(Guid businessId, Guid branchId, [FromBody] AssignEmployeeToBranchRequest request, CancellationToken ct)
    {
        var result = await branchService.AssignEmployeeAsync(branchId, request, ct);
        return Ok(result);
    }

    [HttpDelete("{branchId:guid}/employees/{employeeId:guid}")]
    [RequirePermission("business:write")]
    public async Task<IActionResult> ReleaseEmployee(Guid businessId, Guid branchId, Guid employeeId, CancellationToken ct)
    {
        await branchService.ReleaseEmployeeAsync(branchId, employeeId, ct);
        return NoContent();
    }
}

