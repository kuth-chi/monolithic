using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Sales.Application;
using Monolithic.Api.Modules.Sales.Contracts;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Sales Invoices (AR Invoices) — bill customers and track receivables.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/sales-invoices")]
public sealed class SalesInvoicesController(ISalesInvoiceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
        => Ok(await service.GetByBusinessAsync(businessId, customerId, status, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null || result.BusinessId != businessId ? NotFound() : Ok(result);
    }

    [HttpGet("overdue")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetOverdue(
        Guid businessId,
        [FromQuery] Guid? customerId = null,
        CancellationToken ct = default)
        => Ok(await service.GetOverdueAsync(businessId, customerId, ct));

    [HttpGet("ar-dashboard")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetArDashboard(Guid businessId, CancellationToken ct)
        => Ok(await service.GetDashboardAsync(businessId, ct));

    [HttpPost]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Create(
        Guid businessId,
        [FromBody] CreateSalesInvoiceRequest request,
        CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(request, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPost("{id:guid}/send")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Send(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.SendAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/payments")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> RecordPayment(
        Guid businessId, Guid id,
        [FromBody] RecordSalesPaymentRequest request,
        CancellationToken ct)
    {
        var payment = await service.RecordPaymentAsync(id, request, GetCurrentUserId(), ct);
        return Ok(payment);
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Cancel(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.CancelAsync(id, ct);
        return NoContent();
    }

    [HttpPost("refresh-overdue")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> RefreshOverdue(Guid businessId, CancellationToken ct)
    {
        await service.RefreshOverdueStatusAsync(businessId, ct);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }
}

/// <summary>
/// AR Credit Notes — issue credits to customers, apply to invoices.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/ar/credit-notes")]
public sealed class ArCreditNotesController(IArCreditNoteService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] Guid? customerId = null,
        CancellationToken ct = default)
        => Ok(await service.GetByBusinessAsync(businessId, customerId, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null || result.BusinessId != businessId ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Create(
        Guid businessId,
        [FromBody] CreateArCreditNoteRequest request,
        CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(request, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPost("{id:guid}/confirm")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Confirm(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.ConfirmAsync(id, GetCurrentUserId(), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/apply")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Apply(
        Guid businessId, Guid id,
        [FromBody] ApplyArCreditNoteRequest request,
        CancellationToken ct)
    {
        var result = await service.ApplyAsync(id, request, GetCurrentUserId(), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("finance:write")]
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
