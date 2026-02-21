using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Platform.FeatureFlags.Application;
using Monolithic.Api.Modules.Platform.FeatureFlags.Contracts;
using Monolithic.Api.Modules.Platform.FeatureFlags.Domain;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Feature flag management and real-time evaluation.
///
/// GET    /api/v1/feature-flags                        – list all flags
/// GET    /api/v1/feature-flags/check/{key}            – evaluate a single flag
/// POST   /api/v1/feature-flags                        – upsert a flag
/// DELETE /api/v1/feature-flags/{id}                   – delete a flag
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/feature-flags")]
public sealed class FeatureFlagsController(IFeatureFlagService flagService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("platform:feature-flags:read")]
    public async Task<IActionResult> List(
        [FromQuery] FeatureFlagScope? scope,
        [FromQuery] Guid? businessId,
        CancellationToken ct)
        => Ok(await flagService.ListAsync(scope, businessId, ct));

    /// <summary>
    /// Evaluate a feature flag using scope fall-through without returning
    /// internal flag metadata. Safe to expose to front-end clients.
    /// </summary>
    [HttpGet("check/{key}")]
    [RequirePermission("platform:feature-flags:read")]
    public async Task<IActionResult> Check(
        string key,
        [FromQuery] Guid? businessId,
        [FromQuery] Guid? userId,
        CancellationToken ct)
        => Ok(await flagService.CheckAsync(key, businessId, userId, ct));

    [HttpPost]
    [RequirePermission("platform:feature-flags:write")]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertFeatureFlagRequest req, CancellationToken ct)
        => Ok(await flagService.UpsertAsync(req, ct));

    [HttpDelete("{id:guid}")]
    [RequirePermission("platform:feature-flags:write")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await flagService.DeleteAsync(id, ct);
        return NoContent();
    }
}
