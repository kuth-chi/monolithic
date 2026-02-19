using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Analytics.Application;
using Monolithic.Api.Modules.Analytics.Contracts;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/dashboard")]
public sealed class DashboardController(IDashboardQueryService dashboardQueryService) : ControllerBase
{
    [HttpGet("realtime")]
    [ProducesResponseType<RealtimeDashboardSnapshotDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRealtime(CancellationToken cancellationToken)
    {
        var snapshot = await dashboardQueryService.GetRealtimeSnapshotAsync(cancellationToken);
        return Ok(snapshot);
    }
}