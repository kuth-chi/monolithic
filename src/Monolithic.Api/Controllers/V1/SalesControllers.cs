using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Sales.Application;
using Monolithic.Api.Modules.Sales.Contracts;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Customer Quotations — create, update, send and convert to Sales Orders.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/quotations")]
public sealed class QuotationsController(IQuotationService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission("sales:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
        => Ok(await service.GetByBusinessAsync(businessId, customerId, status, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("sales:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null || result.BusinessId != businessId ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("sales:write")]
    public async Task<IActionResult> Create(
        Guid businessId,
        [FromBody] CreateQuotationRequest request,
        CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(request, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("sales:write")]
    public async Task<IActionResult> Update(
        Guid businessId, Guid id,
        [FromBody] UpdateQuotationRequest request,
        CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/send")]
    [RequirePermission("sales:write")]
    public async Task<IActionResult> Send(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.SendAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/convert")]
    [RequirePermission("sales:write")]
    public async Task<IActionResult> Convert(
        Guid businessId, Guid id,
        [FromBody] ConvertQuotationRequest request,
        CancellationToken ct)
    {
        var result = await service.ConvertToOrderAsync(id, request, GetCurrentUserId(), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("sales:write")]
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

/// <summary>
/// Sales Orders — confirm customer orders and track fulfillment.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/sales-orders")]
public sealed class SalesOrdersController(ISalesOrderService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission("sales:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
        => Ok(await service.GetByBusinessAsync(businessId, customerId, status, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("sales:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null || result.BusinessId != businessId ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("sales:write")]
    public async Task<IActionResult> Create(
        Guid businessId,
        [FromBody] CreateSalesOrderRequest request,
        CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(request, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("sales:write")]
    public async Task<IActionResult> Update(
        Guid businessId, Guid id,
        [FromBody] UpdateSalesOrderRequest request,
        CancellationToken ct)
        => Ok(await service.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/confirm")]
    [RequirePermission("sales:write")]
    public async Task<IActionResult> Confirm(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.ConfirmAsync(id, GetCurrentUserId(), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("sales:write")]
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
