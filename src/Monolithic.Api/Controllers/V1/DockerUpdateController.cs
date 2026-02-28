using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Platform.Contracts;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Docker Image Update Controller — Admin only
///
/// Allows an admin to check whether newer Docker images are available on
/// Docker Hub for configured services, and to trigger a rolling update.
///
/// Routes:
///   GET  /api/v1/admin/docker/update-check   — compare local vs Docker Hub digests
///   POST /api/v1/admin/docker/apply-update   — dispatch detached pull + restart
///
/// Security:
///   • Requires permission "platform:docker:update" (Admin role only).
///   • Apply endpoint is rate-limited by the global API rate limiter.
///   • Endpoint is excluded from LicenseValidationMiddleware naturally
///     (only accessible when user is already authenticated and licensed).
///
/// Infrastructure requirement:
///   • /var/run/docker.sock must be mounted into the API container.
///   • docker CLI must be installed in the runtime image.
///   See docker-compose.yml and Dockerfile for the necessary changes.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/admin/docker")]
public sealed class DockerUpdateController(
    IDockerUpdateService dockerUpdateService,
    DockerUpdateOptions dockerOptions) : ControllerBase
{
    // ── GET /api/v1/admin/docker/update-check ─────────────────────────────────

    /// <summary>
    /// Checks all configured services against Docker Hub.
    /// Uses 10-minute cache to avoid Docker Hub rate limits.
    /// Returns 503 when Docker Hub is unreachable (body explains state).
    /// </summary>
    [HttpGet("update-check")]
    [RequirePermission("platform:docker:update")]
    [ProducesResponseType<DockerUpdateCheckResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CheckUpdates(CancellationToken ct)
    {
        if (!dockerOptions.Enabled)
            return StatusCode(503, new { message = "Docker update feature is disabled on this instance." });

        var result = await dockerUpdateService.CheckUpdatesAsync(ct);
        return Ok(result);
    }

    // ── POST /api/v1/admin/docker/apply-update ────────────────────────────────

    /// <summary>
    /// Applies updates for the requested services.
    /// Dispatches a detached background process (nohup) so this request
    /// returns immediately.  The update runs after a 3-second delay.
    ///
    /// The affected container(s) will restart.  If the API container is one of
    /// the targets, the API will briefly go offline and come back once Docker
    /// pulls and recreates the container.
    ///
    /// Returns 422 when no valid service names are provided.
    /// Returns 503 when the Docker socket is not mounted.
    /// </summary>
    [HttpPost("apply-update")]
    [RequirePermission("platform:docker:update")]
    [ProducesResponseType<ApplyUpdateResult>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ApplyUpdate(
        [FromBody] ApplyUpdateRequest request,
        CancellationToken ct)
    {
        if (!dockerOptions.Enabled)
            return StatusCode(503, new { message = "Docker update feature is disabled on this instance." });

        if (request.ServiceNames is null || request.ServiceNames.Count == 0)
            return UnprocessableEntity(new { message = "Provide at least one service name, or [\"*\"] for all." });

        var result = await dockerUpdateService.ApplyUpdatesAsync(request.ServiceNames, ct);

        if (!result.Accepted)
        {
            // 503 when socket missing; 422 for config/service mismatch
            var statusCode = result.Message.Contains("socket", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status422UnprocessableEntity;
            return StatusCode(statusCode, result);
        }

        // 202 Accepted — update is in progress asynchronously
        return Accepted(result);
    }
}
