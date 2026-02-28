using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Identity.Authorization;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// License Status Controller — lightweight polling endpoint for the frontend.
///
/// Routes:
///   GET  /api/v1/license/status
///       Returns the current guard result using the local DB (fast path).
///       Does NOT trigger a remote re-validation.
///
///   POST /api/v1/license/validate-remote
///       Triggers an on-demand remote validation.
///       Rate-limited by the global API rate limiter.
///       Returns the updated guard result.
///
/// Both endpoints are outside the scope of <see cref="LicenseValidationMiddleware"/>
/// (bypassed by the "/api/v1/license/" prefix) so the UI can always read the
/// status even when the license is locked.
///
/// Authentication is REQUIRED — the guard resolves the license by the JWT sub claim.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/license")]
[Authorize]
public sealed class LicenseStatusController(
    ILicenseGuardService guardService) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            out var id) ? id : Guid.Empty;

    // ── GET /api/v1/license/status ────────────────────────────────────────────

    /// <summary>
    /// Returns the local license status for the authenticated user.
    /// Fast path — no network call.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType<LicenseStatusDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await guardService.GetLocalStatusAsync(CurrentUserId, ct);
        return Ok(MapToDto(result));
    }

    // ── POST /api/v1/license/validate-remote ──────────────────────────────────

    /// <summary>
    /// Triggers an on-demand remote validation.
    /// Fetches the GitHub license mapping and updates the local DB state.
    /// Returns 503 when the remote is unreachable (the frontend shows a toast).
    /// </summary>
    [HttpPost("validate-remote")]
    [RequirePermission("owner:write")]
    [ProducesResponseType<LicenseStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ValidateRemote(CancellationToken ct)
    {
        var result = await guardService.ValidateRemoteAsync(CurrentUserId, ct);

        if (result.Code == LicenseGuardCode.RemoteUnreachable)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                message = "Remote license server is unreachable. Validation will retry automatically.",
                code    = LicenseGuardCode.RemoteUnreachable,
            });
        }

        return Ok(MapToDto(result));
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static LicenseStatusDto MapToDto(LicenseGuardResult r) => new(
        IsValid:                  r.IsValid,
        Code:                     r.Code,
        Message:                  r.Message,
        Plan:                     r.Plan?.ToString(),
        ExpiresOn:                r.ExpiresOn?.ToString("yyyy-MM-dd"),
        DaysUntilExpiry:          r.DaysUntilExpiry,
        IsExpiringSoon:           r.IsExpiringSoon,
        LastRemoteValidatedAtUtc: r.LastRemoteValidatedAtUtc);
}
