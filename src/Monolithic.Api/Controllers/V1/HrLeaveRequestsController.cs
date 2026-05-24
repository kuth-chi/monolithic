using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Hr.Application;
using Monolithic.Api.Modules.Hr.Contracts;
using Monolithic.Api.Modules.Identity.Authorization;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/businesses/{businessId:guid}/hr/leave-requests")]
public sealed class HrLeaveRequestsController(IHrLeaveService leaveService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("hr:leave:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var rows = await leaveService.GetByBusinessAsync(businessId, status, ct);
        return Ok(rows);
    }

    [HttpPost]
    [RequirePermission("hr:leave:write")]
    public async Task<IActionResult> Create(
        Guid businessId,
        [FromBody] CreateLeaveRequest request,
        CancellationToken ct)
    {
        try
        {
            var created = await leaveService.CreateAsync(businessId, request, ct);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid leave request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Leave request validation failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
            });
        }
    }

    [HttpPost("{leaveRequestId:guid}/approve")]
    [RequirePermission("hr:leave:approve")]
    public async Task<IActionResult> Approve(
        Guid businessId,
        Guid leaveRequestId,
        [FromBody] ReviewLeaveRequest request,
        CancellationToken ct)
    {
        var reviewed = await leaveService.ApproveAsync(
            businessId,
            leaveRequestId,
            GetCurrentUserId(),
            request.Comment,
            ct);

        return Ok(reviewed);
    }

    [HttpPost("{leaveRequestId:guid}/reject")]
    [RequirePermission("hr:leave:approve")]
    public async Task<IActionResult> Reject(
        Guid businessId,
        Guid leaveRequestId,
        [FromBody] ReviewLeaveRequest request,
        CancellationToken ct)
    {
        var reviewed = await leaveService.RejectAsync(
            businessId,
            leaveRequestId,
            GetCurrentUserId(),
            request.Comment,
            ct);

        return Ok(reviewed);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        return Guid.TryParse(claim?.Value, out var userId) ? userId : Guid.Empty;
    }
}
