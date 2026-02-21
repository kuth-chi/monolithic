using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Platform.Notifications.Application;
using Monolithic.Api.Modules.Platform.Notifications.Contracts;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Multi-channel notification dispatch and audit log.
///
/// POST   /api/v1/notifications/send   – send via Email/SMS/Push/InApp
/// GET    /api/v1/notifications        – paginated notification log
/// POST   /api/v1/notifications/{id}/retry – retry a failed notification
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/notifications")]
public sealed class NotificationsController(INotificationService notifService) : ControllerBase
{
    /// <summary>
    /// Send a notification on any channel.
    /// Template is resolved by slug; variables are injected via Scriban rendering.
    /// </summary>
    [HttpPost("send")]
    [RequirePermission("platform:notifications:write")]
    public async Task<IActionResult> Send(
        [FromBody] SendNotificationRequest req, CancellationToken ct)
        => Ok(await notifService.SendAsync(req, ct));

    [HttpGet]
    [RequirePermission("platform:notifications:read")]
    public async Task<IActionResult> List(
        [FromQuery] NotificationListRequest req, CancellationToken ct)
        => Ok(await notifService.ListLogsAsync(req, ct));

    [HttpPost("{id:guid}/retry")]
    [RequirePermission("platform:notifications:write")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        await notifService.RetryFailedAsync(id, ct);
        return NoContent();
    }
}
