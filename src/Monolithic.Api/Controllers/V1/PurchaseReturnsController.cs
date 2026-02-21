using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Purchases.Application;
using Monolithic.Api.Modules.Purchases.Contracts;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Purchase Returns — manage returns of goods back to vendors.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/purchase-returns")]
public sealed class PurchaseReturnsController(IPurchaseReturnService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission("purchase:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] Guid? vendorId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
        => Ok(await service.GetByBusinessAsync(businessId, vendorId, status, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("purchase:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null || result.BusinessId != businessId ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("purchase:write")]
    public async Task<IActionResult> Create(
        Guid businessId,
        [FromBody] CreatePurchaseReturnRequest request,
        CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(request, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPost("{id:guid}/confirm")]
    [RequirePermission("purchase:write")]
    public async Task<IActionResult> Confirm(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.ConfirmAsync(id, GetCurrentUserId(), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-shipped")]
    [RequirePermission("purchase:write")]
    public async Task<IActionResult> MarkShipped(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.MarkShippedAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/record-vendor-credit")]
    [RequirePermission("purchase:write")]
    public async Task<IActionResult> RecordVendorCredit(
        Guid businessId, Guid id,
        [FromBody] RecordVendorCreditRequest request,
        CancellationToken ct)
    {
        await service.RecordVendorCreditAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("purchase:write")]
    public async Task<IActionResult> Cancel(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.CancelAsync(id, ct);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }
}
