using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Hr.Application;
using Monolithic.Api.Modules.Hr.Contracts;
using Monolithic.Api.Modules.Identity.Authorization;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/businesses/{businessId:guid}/hr/payroll-runs")]
public sealed class HrPayrollController(IHrPayrollService payrollService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("hr:payroll:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var rows = await payrollService.GetByBusinessAsync(businessId, status, ct);
        return Ok(rows);
    }

    [HttpPost]
    [RequirePermission("hr:payroll:write")]
    public async Task<IActionResult> Create(
        Guid businessId,
        [FromBody] CreatePayrollRunRequest request,
        CancellationToken ct)
    {
        var created = await payrollService.CreateAsync(businessId, request, ct);
        return Ok(created);
    }

    [HttpPost("{payrollRunId:guid}/submit")]
    [RequirePermission("hr:payroll:write")]
    public async Task<IActionResult> Submit(Guid businessId, Guid payrollRunId, CancellationToken ct)
        => Ok(await payrollService.SubmitAsync(businessId, payrollRunId, ct));

    [HttpPost("{payrollRunId:guid}/approve")]
    [RequirePermission("hr:payroll:approve")]
    public async Task<IActionResult> Approve(
        Guid businessId,
        Guid payrollRunId,
        [FromBody] ReviewPayrollRunRequest request,
        CancellationToken ct)
        => Ok(await payrollService.ApproveAsync(businessId, payrollRunId, GetCurrentUserId(), request.Comment, ct));

    [HttpPost("{payrollRunId:guid}/finalize")]
    [RequirePermission("hr:payroll:approve")]
    public async Task<IActionResult> Finalize(
        Guid businessId,
        Guid payrollRunId,
        [FromBody] ReviewPayrollRunRequest request,
        CancellationToken ct)
        => Ok(await payrollService.FinalizeAsync(businessId, payrollRunId, GetCurrentUserId(), request.Comment, ct));

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        return Guid.TryParse(claim?.Value, out var userId) ? userId : Guid.Empty;
    }
}
