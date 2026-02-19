using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Branch management within a business.
/// Enforces: ≥1 branch, exactly one HQ, branch count ≤ license quota.
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

    [HttpGet]
    [RequirePermission("business:read")]
    public async Task<IActionResult> GetAll(Guid businessId, CancellationToken ct)
        => Ok(await branchService.GetByBusinessAsync(businessId, ct));

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

    [HttpGet("{branchId:guid}/employees")]
    [RequirePermission("business:read")]
    public async Task<IActionResult> GetEmployees(Guid businessId, Guid branchId, CancellationToken ct)
        => Ok(await branchService.GetEmployeesAsync(branchId, ct));

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
