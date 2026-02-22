using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.UserPreferences.Application;
using Monolithic.Api.Modules.Platform.UserPreferences.Contracts;
using Monolithic.Api.Modules.Platform.UserPreferences.Domain;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// User preference management including timezone, pagination size, and dashboard widget layout.
///
/// Self-service (caller manages their own data):
///   GET    /api/v1/preferences/me                  – get or create my preferences
///   PUT    /api/v1/preferences/me                  – update my preferences (timezone, page size, etc.)
///   PUT    /api/v1/preferences/me/layout           – update my dashboard layout only
///   DELETE /api/v1/preferences/me/layout           – reset my layout to defaults
///
/// Discovery (no mutation):
///   GET    /api/v1/preferences/timezones           – list all supported IANA timezone IDs
///   GET    /api/v1/preferences/widgets             – list all available dashboard widgets
///
/// Admin (manage other users):
///   GET    /api/v1/preferences/{userId}            – get or create preferences for a user
///   PUT    /api/v1/preferences/{userId}            – update preferences for a user
///   PUT    /api/v1/preferences/{userId}/layout     – update dashboard layout for a user
///   DELETE /api/v1/preferences/{userId}/layout     – reset layout for a user
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/preferences")]
public sealed class UserPreferencesController(
    IUserPreferenceService prefService,
    ITenantContext tenant) : ControllerBase
{
    // ── Discovery endpoints (no auth required) ────────────────────────────────

    /// <summary>Returns all IANA/system timezone IDs supported by the server.</summary>
    [HttpGet("timezones")]
    public IActionResult GetTimezones()
        => Ok(prefService.GetAvailableTimezones());

    /// <summary>Returns all registerable dashboard widget descriptors.</summary>
    [HttpGet("widgets")]
    public IActionResult GetWidgets()
        => Ok(prefService.GetAvailableWidgets());

    // ── Self-service ("me") ───────────────────────────────────────────────────

    /// <summary>
    /// Get (or lazily create) the calling user's own preferences.
    /// Timezone and pagination size are included in the response.
    /// </summary>
    [HttpGet("me")]
    [RequirePermission("platform:preferences:read")]
    public async Task<IActionResult> GetMy([FromQuery] Guid? businessId, CancellationToken ct)
    {
        var userId = RequireCurrentUserId();
        return Ok(await prefService.GetOrCreateAsync(userId, businessId ?? tenant.BusinessId, ct));
    }

    /// <summary>
    /// Update the calling user's own preferences.
    /// Accepted changes: timezone (IANA), locale, theme, color-scheme,
    /// defaultPageSize (5–100), widget layout, notification opt-ins.
    /// </summary>
    [HttpPut("me")]
    [RequirePermission("platform:preferences:write")]
    public async Task<IActionResult> UpdateMy([FromBody] UpdateMyPreferenceRequest req, CancellationToken ct)
    {
        var userId = RequireCurrentUserId();
        try
        {
            return Ok(await prefService.UpdateMyAsync(userId, req, ct));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    /// <summary>Replace the calling user's dashboard widget layout.</summary>
    [HttpPut("me/layout")]
    [RequirePermission("platform:preferences:write")]
    public async Task<IActionResult> UpdateMyLayout([FromBody] UpdateMyDashboardLayoutRequest req, CancellationToken ct)
    {
        var userId = RequireCurrentUserId();
        return Ok(await prefService.UpdateMyLayoutAsync(userId, req, ct));
    }

    /// <summary>Reset the calling user's dashboard layout to system defaults.</summary>
    [HttpDelete("me/layout")]
    [RequirePermission("platform:preferences:write")]
    public async Task<IActionResult> ResetMyLayout([FromQuery] Guid? businessId, CancellationToken ct)
    {
        var userId = RequireCurrentUserId();
        await prefService.ResetMyLayoutAsync(userId, businessId ?? tenant.BusinessId, ct);
        return NoContent();
    }

    // ── Admin: manage any user ────────────────────────────────────────────────

    /// <summary>Get (or lazily create) preferences for any user. Requires admin permission.</summary>
    [HttpGet("{userId:guid}")]
    [RequirePermission("platform:preferences:admin")]
    public async Task<IActionResult> Get(
        Guid userId, [FromQuery] Guid? businessId, CancellationToken ct)
        => Ok(await prefService.GetOrCreateAsync(userId, businessId ?? tenant.BusinessId, ct));

    /// <summary>Update preferences for any user. Requires admin permission.</summary>
    [HttpPut("{userId:guid}")]
    [RequirePermission("platform:preferences:admin")]
    public async Task<IActionResult> Update(
        Guid userId, [FromBody] UpdateUserPreferenceRequest req, CancellationToken ct)
    {
        if (req.UserId != userId) return BadRequest(new { error = "UserId in body does not match route parameter." });
        try
        {
            return Ok(await prefService.UpdateAsync(req, ct));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    /// <summary>Replace the dashboard layout for any user. Requires admin permission.</summary>
    [HttpPut("{userId:guid}/layout")]
    [RequirePermission("platform:preferences:admin")]
    public async Task<IActionResult> UpdateLayout(
        Guid userId, [FromBody] UpdateDashboardLayoutRequest req, CancellationToken ct)
    {
        if (req.UserId != userId) return BadRequest(new { error = "UserId in body does not match route parameter." });
        return Ok(await prefService.UpdateLayoutAsync(req, ct));
    }

    /// <summary>Reset the dashboard layout for any user. Requires admin permission.</summary>
    [HttpDelete("{userId:guid}/layout")]
    [RequirePermission("platform:preferences:admin")]
    public async Task<IActionResult> ResetLayout(
        Guid userId, [FromQuery] Guid? businessId, CancellationToken ct)
    {
        await prefService.ResetLayoutAsync(userId, businessId ?? tenant.BusinessId, ct);
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid RequireCurrentUserId()
    {
        return tenant.UserId
            ?? throw new InvalidOperationException("Authenticated user context is missing.");
    }
}
