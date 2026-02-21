using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.UserPreferences.Application;
using Monolithic.Api.Modules.Platform.UserPreferences.Contracts;
using Monolithic.Api.Modules.Platform.UserPreferences.Domain;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// User preference management including dashboard widget layout.
///
/// GET    /api/v1/preferences/{userId}             – get or create preferences
/// PUT    /api/v1/preferences/{userId}             – update preferences
/// PUT    /api/v1/preferences/{userId}/layout      – update dashboard layout only
/// DELETE /api/v1/preferences/{userId}/layout      – reset layout to defaults
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/preferences")]
public sealed class UserPreferencesController(
    IUserPreferenceService prefService,
    ITenantContext tenant) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    [RequirePermission("platform:preferences:read")]
    public async Task<IActionResult> Get(
        Guid userId, [FromQuery] Guid? businessId, CancellationToken ct)
        => Ok(await prefService.GetOrCreateAsync(userId, businessId ?? tenant.BusinessId, ct));

    [HttpPut("{userId:guid}")]
    [RequirePermission("platform:preferences:write")]
    public async Task<IActionResult> Update(
        Guid userId, [FromBody] UpdateUserPreferenceRequest req, CancellationToken ct)
    {
        if (req.UserId != userId) return BadRequest("UserId mismatch.");
        return Ok(await prefService.UpdateAsync(req, ct));
    }

    [HttpPut("{userId:guid}/layout")]
    [RequirePermission("platform:preferences:write")]
    public async Task<IActionResult> UpdateLayout(
        Guid userId, [FromBody] UpdateDashboardLayoutRequest req, CancellationToken ct)
    {
        if (req.UserId != userId) return BadRequest("UserId mismatch.");
        return Ok(await prefService.UpdateLayoutAsync(req, ct));
    }

    [HttpDelete("{userId:guid}/layout")]
    [RequirePermission("platform:preferences:write")]
    public async Task<IActionResult> ResetLayout(
        Guid userId, [FromQuery] Guid? businessId, CancellationToken ct)
    {
        await prefService.ResetLayoutAsync(userId, businessId ?? tenant.BusinessId, ct);
        return NoContent();
    }
}
