using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Vendor Bills (AP Invoices) — create, confirm, pay, and track overdue bills per vendor.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/vendor-bills")]
public sealed class VendorBillsController(IVendorBillService billService) : ControllerBase
{
    // ── List / Query ──────────────────────────────────────────────────────────
    [HttpGet]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] string? status = null,
        [FromQuery] Guid? vendorId = null,
        CancellationToken ct = default)
        => Ok(await billService.GetByBusinessAsync(businessId, status, vendorId, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await billService.GetByIdAsync(id, ct);
        return result is null || result.BusinessId != businessId ? NotFound() : Ok(result);
    }

    // ── Overdue ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a summary of overdue amounts grouped by vendor.
    /// The primary endpoint for the accountant's overdue billing dashboard.
    /// </summary>
    [HttpGet("overdue/by-vendor")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetOverdueSummaryByVendor(Guid businessId, CancellationToken ct)
        => Ok(await billService.GetOverdueSummaryByVendorAsync(businessId, ct));

    /// <summary>
    /// Returns all individual overdue bills (optionally filtered by vendor).
    /// </summary>
    [HttpGet("overdue")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetOverdueBills(
        Guid businessId,
        [FromQuery] Guid? vendorId = null,
        CancellationToken ct = default)
        => Ok(await billService.GetOverdueBillsAsync(businessId, vendorId, ct));

    // ── Create ────────────────────────────────────────────────────────────────
    [HttpPost]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Create(
        Guid businessId,
        [FromBody] CreateVendorBillRequest request,
        CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var userId = GetCurrentUserId();
        var result = await billService.CreateAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    // ── State Transitions ─────────────────────────────────────────────────────
    [HttpPost("{id:guid}/confirm")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Confirm(Guid businessId, Guid id, CancellationToken ct)
    {
        await billService.ConfirmAsync(id, GetCurrentUserId(), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Cancel(Guid businessId, Guid id, [FromQuery] string reason = "", CancellationToken ct = default)
    {
        await billService.CancelAsync(id, reason, ct);
        return NoContent();
    }

    // ── Payments ──────────────────────────────────────────────────────────────
    [HttpPost("{id:guid}/payments")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> RecordPayment(
        Guid businessId,
        Guid id,
        [FromBody] RecordVendorBillPaymentRequest request,
        CancellationToken ct)
    {
        var payment = await billService.RecordPaymentAsync(id, request, GetCurrentUserId(), ct);
        return Ok(payment);
    }

    // ── Admin / Background ────────────────────────────────────────────────────
    /// <summary>
    /// Recalculates overdue status and DaysOverdue for all open bills. Run daily.
    /// </summary>
    [HttpPost("refresh-overdue")]
    [RequirePermission("finance:admin")]
    public async Task<IActionResult> RefreshOverdue(Guid businessId, CancellationToken ct)
    {
        await billService.RefreshOverdueStatusAsync(businessId, ct);
        return Ok(new { message = "Overdue status refreshed.", businessId });
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }
}
