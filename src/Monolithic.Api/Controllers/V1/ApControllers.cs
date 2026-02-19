using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Authorization;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// AP Dashboard — outstanding and overdue liabilities per vendor for a business.
/// Primary entry point for the Accounts Payable manager.
///   GET /api/v1/businesses/{id}/ap/dashboard  → full AP summary (all vendors)
///   GET /api/v1/businesses/{id}/ap/vendors/{vendorId}/summary → one vendor
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/ap")]
public sealed class ApDashboardController(IApDashboardService dashboardService) : ControllerBase
{
    [HttpGet("dashboard")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetDashboard(Guid businessId, CancellationToken ct)
        => Ok(await dashboardService.GetDashboardAsync(businessId, ct));

    [HttpGet("vendors/{vendorId:guid}/summary")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetVendorSummary(Guid businessId, Guid vendorId, CancellationToken ct)
    {
        var result = await dashboardService.GetVendorSummaryAsync(businessId, vendorId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Vendor Credit Terms — named credit term templates per business.
///   GET    /api/v1/businesses/{id}/ap/credit-terms
///   GET    /api/v1/businesses/{id}/ap/credit-terms/{id}
///   POST   /api/v1/businesses/{id}/ap/credit-terms
///   PUT    /api/v1/businesses/{id}/ap/credit-terms/{id}
///   DELETE /api/v1/businesses/{id}/ap/credit-terms/{id}
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/ap/credit-terms")]
public sealed class VendorCreditTermsController(IVendorCreditTermService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetAll(Guid businessId, CancellationToken ct)
        => Ok(await service.GetByBusinessAsync(businessId, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Create(Guid businessId, [FromBody] CreateVendorCreditTermRequest req, CancellationToken ct)
    {
        if (req.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Update(Guid businessId, Guid id, [FromBody] UpdateVendorCreditTermRequest req, CancellationToken ct)
        => Ok(await service.UpdateAsync(id, req, ct));

    [HttpDelete("{id:guid}")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Delete(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Vendor Classes — classification / grading system per business.
///   GET    /api/v1/businesses/{id}/ap/vendor-classes
///   GET    /api/v1/businesses/{id}/ap/vendor-classes/{id}
///   POST   /api/v1/businesses/{id}/ap/vendor-classes
///   PUT    /api/v1/businesses/{id}/ap/vendor-classes/{id}
///   DELETE /api/v1/businesses/{id}/ap/vendor-classes/{id}
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/ap/vendor-classes")]
public sealed class VendorClassesController(IVendorClassService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetAll(Guid businessId, CancellationToken ct)
        => Ok(await service.GetByBusinessAsync(businessId, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Create(Guid businessId, [FromBody] CreateVendorClassRequest req, CancellationToken ct)
    {
        if (req.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Update(Guid businessId, Guid id, [FromBody] UpdateVendorClassRequest req, CancellationToken ct)
        => Ok(await service.UpdateAsync(id, req, ct));

    [HttpDelete("{id:guid}")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Delete(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Vendor AP Profile — extended AP data per vendor (VAT, credit term, rating, hold/blacklist).
///   GET   /api/v1/businesses/{id}/vendors/{vendorId}/ap-profile
///   PUT   /api/v1/businesses/{id}/vendors/{vendorId}/ap-profile            (upsert)
///   PATCH /api/v1/businesses/{id}/vendors/{vendorId}/ap-profile/hold
///   PATCH /api/v1/businesses/{id}/vendors/{vendorId}/ap-profile/blacklist
///   PATCH /api/v1/businesses/{id}/vendors/{vendorId}/ap-profile/rating
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/vendors/{vendorId:guid}/ap-profile")]
public sealed class VendorApProfileController(IVendorProfileService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> Get(Guid businessId, Guid vendorId, CancellationToken ct)
    {
        var result = await service.GetByVendorAsync(vendorId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Upsert(
        Guid businessId,
        Guid vendorId,
        [FromBody] UpsertVendorProfileRequest req,
        CancellationToken ct)
        => Ok(await service.UpsertAsync(vendorId, req, ct));

    [HttpPatch("hold")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> SetHold(
        Guid businessId,
        Guid vendorId,
        [FromQuery] bool onHold,
        [FromQuery] string reason = "",
        CancellationToken ct = default)
    {
        await service.SetHoldAsync(vendorId, onHold, reason, ct);
        return NoContent();
    }

    [HttpPatch("blacklist")]
    [RequirePermission("finance:admin")]
    public async Task<IActionResult> SetBlacklist(
        Guid businessId,
        Guid vendorId,
        [FromQuery] bool blacklisted,
        [FromQuery] string reason = "",
        CancellationToken ct = default)
    {
        await service.SetBlacklistAsync(vendorId, blacklisted, reason, ct);
        return NoContent();
    }

    [HttpPatch("rating")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> UpdateRating(
        Guid businessId,
        Guid vendorId,
        [FromQuery] decimal rating,
        CancellationToken ct = default)
    {
        await service.UpdateRatingAsync(vendorId, rating, ct);
        return NoContent();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// AP Payment Sessions — bulk or selected-bill payment runs.
///
/// Workflow:
///   1. POST  /prepare  → Draft session (bills allocated, not committed)
///   2. POST  /{id}/post → Apply payments (VendorBillPayments created, bills updated)
///   3. POST  /{id}/reverse → Undo a posted session
///
/// Mode 1 — Bulk (auto oldest-first):   body.paymentMode = "BulkBillPayment",  provide totalAmount
/// Mode 2 — Selected bills:              body.paymentMode = "SelectedBillPayment", provide bills[]
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/ap/payment-sessions")]
public sealed class ApPaymentSessionsController(IApPaymentSessionService service) : ControllerBase
{
    [HttpGet("vendor/{vendorId:guid}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetByVendor(Guid businessId, Guid vendorId, CancellationToken ct)
        => Ok(await service.GetByVendorAsync(businessId, vendorId, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Prepare a draft payment session. Does NOT apply payments yet.
    /// Review the returned Draft before calling POST /{id}/post.
    /// </summary>
    [HttpPost("prepare")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Prepare(
        Guid businessId,
        [FromBody] CreateApPaymentSessionRequest req,
        CancellationToken ct)
    {
        if (req.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var userId = GetCurrentUserId();
        var result = await service.PrepareAsync(req, userId, ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    /// <summary>
    /// Posts the draft session: applies payments to bills. Idempotent.
    /// </summary>
    [HttpPost("{id:guid}/post")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Post(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.PostAsync(id, GetCurrentUserId(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Reverses a posted session. Voids payments and restores bill balances.
    /// </summary>
    [HttpPost("{id:guid}/reverse")]
    [RequirePermission("finance:admin")]
    public async Task<IActionResult> Reverse(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.ReverseAsync(id, GetCurrentUserId(), ct);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// AP Credit Notes — vendor refunds, goods returns, price adjustments.
///   GET    /api/v1/businesses/{id}/ap/vendors/{vendorId}/credit-notes
///   GET    /api/v1/businesses/{id}/ap/credit-notes/{id}
///   POST   /api/v1/businesses/{id}/ap/credit-notes
///   POST   /api/v1/businesses/{id}/ap/credit-notes/{id}/confirm
///   POST   /api/v1/businesses/{id}/ap/credit-notes/{id}/cancel
///   POST   /api/v1/businesses/{id}/ap/credit-notes/{id}/apply   → apply to bill
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/ap/credit-notes")]
public sealed class ApCreditNotesController(IApCreditNoteService service) : ControllerBase
{
    [HttpGet("vendor/{vendorId:guid}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetByVendor(Guid businessId, Guid vendorId, CancellationToken ct)
        => Ok(await service.GetByVendorAsync(businessId, vendorId, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Create(Guid businessId, [FromBody] CreateApCreditNoteRequest req, CancellationToken ct)
    {
        if (req.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(req, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPost("{id:guid}/confirm")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Confirm(Guid businessId, Guid id, CancellationToken ct)
        => Ok(await service.ConfirmAsync(id, GetCurrentUserId(), ct));

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Cancel(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.CancelAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Applies credit note balance to reduce an outstanding vendor bill's AmountDue.
    /// </summary>
    [HttpPost("{id:guid}/apply")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Apply(
        Guid businessId,
        Guid id,
        [FromBody] ApplyCreditNoteRequest req,
        CancellationToken ct)
        => Ok(await service.ApplyToBillAsync(id, req, GetCurrentUserId(), ct));

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// AP Payment Schedules — pay-later for bills.
///   GET    /api/v1/businesses/{id}/ap/payment-schedules/vendor/{vendorId}
///   GET    /api/v1/businesses/{id}/ap/payment-schedules/due   → due today or before date
///   GET    /api/v1/businesses/{id}/ap/payment-schedules/{id}
///   POST   /api/v1/businesses/{id}/ap/payment-schedules
///   PUT    /api/v1/businesses/{id}/ap/payment-schedules/{id}
///   POST   /api/v1/businesses/{id}/ap/payment-schedules/{id}/cancel
///   POST   /api/v1/businesses/{id}/ap/payment-schedules/{id}/execute  → create+post session
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/ap/payment-schedules")]
public sealed class ApPaymentSchedulesController(IApPaymentScheduleService service) : ControllerBase
{
    [HttpGet("vendor/{vendorId:guid}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetByVendor(Guid businessId, Guid vendorId, CancellationToken ct)
        => Ok(await service.GetByVendorAsync(businessId, vendorId, ct));

    [HttpGet("due")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetDue(
        Guid businessId,
        [FromQuery] DateOnly? asOfDate = null,
        CancellationToken ct = default)
        => Ok(await service.GetDueAsync(businessId, asOfDate, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Create(
        Guid businessId,
        [FromBody] CreateApPaymentScheduleRequest req,
        CancellationToken ct)
    {
        if (req.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(req, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Update(Guid businessId, Guid id, [FromBody] UpdateApPaymentScheduleRequest req, CancellationToken ct)
        => Ok(await service.UpdateAsync(id, req, ct));

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Cancel(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.CancelAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Executes the scheduled payment immediately: creates a payment session and posts it.
    /// Returns the posted ApPaymentSession.
    /// </summary>
    [HttpPost("{id:guid}/execute")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Execute(Guid businessId, Guid id, CancellationToken ct)
        => Ok(await service.ExecuteAsync(id, GetCurrentUserId(), ct));

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }
}
