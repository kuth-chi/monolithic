using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Health endpoints for Kubernetes probes and monitoring dashboards.
///
///   GET /health/live    → liveness  (is the process alive?)
///   GET /health/ready   → readiness (are all dependencies up?)
///   GET /health/detail  → full report with per-check status + metadata
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("health")]
public sealed class HealthController(HealthCheckService healthCheckService, IWebHostEnvironment env) : ControllerBase
{
    // ── Liveness ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Returns 200 immediately if the process is running.
    /// No dependency checks — used by Kubernetes to restart crashed pods.
    /// </summary>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Liveness() =>
        Ok(new { status = "alive", timestamp = DateTime.UtcNow });

    // ── Readiness ────────────────────────────────────────────────────────────
    /// <summary>
    /// Returns 200 when all "ready" tagged checks pass, 503 otherwise.
    /// Used by Kubernetes / load-balancers to route traffic.
    /// </summary>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Readiness(CancellationToken ct)
    {
        var report = await healthCheckService.CheckHealthAsync(
            r => r.Tags.Contains("ready"), ct);

        return report.Status == HealthStatus.Healthy
            ? Ok(BuildSummary(report))
            : StatusCode(StatusCodes.Status503ServiceUnavailable, BuildSummary(report));
    }

    // ── Detailed report ───────────────────────────────────────────────────────
    /// <summary>
    /// Full per-check breakdown — intended for monitoring dashboards and ops tooling.
    /// Returns 200 even when degraded/unhealthy so consumers can always read the body.
    /// </summary>
    [HttpGet("detail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Detail(CancellationToken ct)
    {
        var report = await healthCheckService.CheckHealthAsync(ct);
        return Ok(BuildDetailedReport(report));
    }

    // ─────────────────────────────────────────────────────────────────────────
    private object BuildSummary(HealthReport report) => new
    {
        status    = report.Status.ToString(),
        timestamp = DateTime.UtcNow,
        duration  = report.TotalDuration.TotalMilliseconds,
    };

    private object BuildDetailedReport(HealthReport report) => new
    {
        status      = report.Status.ToString(),
        environment = env.EnvironmentName,
        timestamp   = DateTime.UtcNow,
        duration    = report.TotalDuration.TotalMilliseconds,
        checks      = report.Entries.Select(e => new
        {
            name        = e.Key,
            status      = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration    = e.Value.Duration.TotalMilliseconds,
            tags        = e.Value.Tags,
            data        = e.Value.Data.Count > 0 ? e.Value.Data : null,
            error       = e.Value.Exception?.Message,
        }),
    };
}
