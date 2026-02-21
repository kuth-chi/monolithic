using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Finance.Application;
using Monolithic.Api.Modules.Finance.Contracts;
using Monolithic.Api.Modules.Identity.Authorization;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Expense Categories — lookup table for expense classification.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/expense-categories")]
public sealed class ExpenseCategoriesController(IExpenseCategoryService service) : ControllerBase
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
        return result is null || result.BusinessId != businessId ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Create(
        Guid businessId,
        [FromBody] CreateExpenseCategoryRequest request,
        CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Update(
        Guid businessId, Guid id,
        [FromBody] UpdateExpenseCategoryRequest request,
        CancellationToken ct)
        => Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Delete(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

/// <summary>
/// Expense Reports — employee expense submission, approval, and reimbursement.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/expenses")]
public sealed class ExpensesController(IExpenseService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
        => Ok(await service.GetByBusinessAsync(businessId, userId, status, ct));

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
        [FromBody] CreateExpenseRequest request,
        CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Update(
        Guid businessId, Guid id,
        [FromBody] UpdateExpenseRequest request,
        CancellationToken ct)
        => Ok(await service.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/submit")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Submit(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.SubmitAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/review")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Review(
        Guid businessId, Guid id,
        [FromBody] ReviewExpenseRequest request,
        CancellationToken ct)
    {
        await service.ReviewAsync(id, request, GetCurrentUserId(), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-paid")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> MarkPaid(Guid businessId, Guid id, CancellationToken ct)
    {
        await service.MarkPaidAsync(id, ct);
        return NoContent();
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
