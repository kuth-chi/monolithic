using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Owner dashboard and multi-business management.
/// An owner can trace all their businesses and aggregate performance here.
/// License quota is validated on create.
/// </summary>
[ApiController]
[Route("api/v1/owner")]
public sealed class BusinessOwnerController(
    IBusinessOwnershipService ownershipService,
    IBusinessLicenseService licenseService) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst("sub")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value,
            out var id) ? id : Guid.Empty;

    // ── License ───────────────────────────────────────────────────────────────

    [HttpGet("license")]
    [RequirePermission("owner:read")]
    public async Task<IActionResult> GetLicense(CancellationToken ct)
        => Ok(await licenseService.GetByOwnerAsync(CurrentUserId, ct));

    [HttpPut("license")]
    [RequirePermission("admin:write")]
    public async Task<IActionResult> UpsertLicense([FromBody] UpsertBusinessLicenseRequest request, CancellationToken ct)
    {
        if (request.OwnerId != CurrentUserId && !User.IsInRole("Admin"))
            return Forbid();
        return Ok(await licenseService.UpsertAsync(request, ct));
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    /// <summary>Returns all businesses owned by the current user, with aggregate stats.</summary>
    [HttpGet("dashboard")]
    [RequirePermission("owner:read")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => Ok(await ownershipService.GetOwnerDashboardAsync(CurrentUserId, ct));

    [HttpGet("businesses")]
    [RequirePermission("owner:read")]
    public async Task<IActionResult> GetOwnedBusinesses(CancellationToken ct)
        => Ok(await ownershipService.GetByOwnerAsync(CurrentUserId, ct));

    // ── Create Business ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new business under the current owner's license.
    /// Automatically provisions: HQ branch, default settings, standard COA.
    /// Returns 403 if the owner's license quota is exceeded.
    /// </summary>
    [HttpPost("businesses")]
    [RequirePermission("owner:write")]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessWithOwnerRequest request, CancellationToken ct)
    {
        if (!await licenseService.CanCreateBusinessAsync(CurrentUserId, ct))
            return StatusCode(403, "License quota exceeded: cannot create more businesses.");

        var result = await ownershipService.CreateBusinessAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(GetOwnedBusinesses), result);
    }

    [HttpDelete("businesses/{businessId:guid}/revoke")]
    [RequirePermission("owner:write")]
    public async Task<IActionResult> RevokeOwnership(Guid businessId, CancellationToken ct)
    {
        await ownershipService.RevokeOwnershipAsync(CurrentUserId, businessId, ct);
        return NoContent();
    }
}
