using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Hr.Application;
using Monolithic.Api.Modules.Hr.Contracts;
using Monolithic.Api.Modules.Identity.Authorization;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/businesses/{businessId:guid}/hr/attendance-records")]
public sealed class HrAttendanceController(IHrAttendanceService attendanceService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("hr:attendance:read")]
    public async Task<IActionResult> GetAll(
        Guid businessId,
        [FromQuery] DateOnly? workDate,
        [FromQuery] Guid? employeeId,
        CancellationToken ct)
    {
        var rows = await attendanceService.GetByBusinessAsync(businessId, workDate, employeeId, ct);
        return Ok(rows);
    }

    [HttpPost("check-in")]
    [RequirePermission("hr:attendance:write")]
    public async Task<IActionResult> CheckIn(
        Guid businessId,
        [FromBody] CheckInAttendanceRequest request,
        CancellationToken ct)
    {
        var created = await attendanceService.CheckInAsync(businessId, request, ct);
        return Ok(created);
    }

    [HttpPost("{attendanceId:guid}/check-out")]
    [RequirePermission("hr:attendance:write")]
    public async Task<IActionResult> CheckOut(
        Guid businessId,
        Guid attendanceId,
        [FromBody] CheckOutAttendanceRequest request,
        CancellationToken ct)
    {
        var updated = await attendanceService.CheckOutAsync(businessId, attendanceId, request, ct);
        return Ok(updated);
    }
}
