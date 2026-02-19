using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Estimate Purchase Orders (Request for Quotation / RFQ).
/// Process: Draft → SentToVendor → VendorQuoteReceived → Approved → ConvertedToPo
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/estimate-purchase-orders")]
public sealed class EstimatePurchaseOrdersController(IEstimatePurchaseOrderService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission("purchasing:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] string? status = null,
        [FromQuery] Guid? vendorId = null,
        CancellationToken ct = default)
        => Ok(await service.GetByBusinessAsync(businessId, status, vendorId, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("purchasing:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null || result.BusinessId != businessId ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("purchasing:write")]
    public async Task<IActionResult> Create(Guid businessId, [FromBody] CreateEstimatePurchaseOrderRequest request, CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(request, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPost("{id:guid}/send-to-vendor")]
    [RequirePermission("purchasing:write")]
    public async Task<IActionResult> SendToVendor(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.SendToVendorAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/record-quote")]
    [RequirePermission("purchasing:write")]
    public async Task<IActionResult> RecordVendorQuote(
        Guid businessId,
        Guid id,
        [FromQuery] DateTimeOffset quoteExpiry,
        [FromQuery] string vendorQuoteRef = "",
        CancellationToken ct = default)
    {
        await service.RecordVendorQuoteAsync(id, quoteExpiry, vendorQuoteRef, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission("purchasing:approve")]
    public async Task<IActionResult> Approve(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.ApproveAsync(id, GetCurrentUserId(), ct);
        return NoContent();
    }

    /// <summary>Converts an Approved RFQ into a Purchase Order.</summary>
    [HttpPost("{id:guid}/convert-to-po")]
    [RequirePermission("purchasing:approve")]
    public async Task<IActionResult> ConvertToPo(
        Guid businessId,
        Guid id,
        [FromBody] ConvertEstimateToPurchaseOrderRequest request,
        CancellationToken ct)
    {
        var po = await service.ConvertToPurchaseOrderAsync(id, request, GetCurrentUserId(), ct);
        return Ok(po);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }
}
