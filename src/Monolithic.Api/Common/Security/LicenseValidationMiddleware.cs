using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Common.Security;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Per-request HTTP middleware that gates access to the API based on the
/// authenticated user's local license state.
///
/// Fast-path (no network): reads <see cref="BusinessLicense"/> from the local
/// database and applies the same logic as <see cref="Application.LicenseGuardService.BuildLocalResult"/>.
/// The remote sweep is performed by <see cref="BackgroundServices.LicenseExpirationMonitorService"/>
/// on a daily schedule — this middleware never makes external HTTP calls.
///
/// Bypassed routes (no license check):
///   • /api/v1/auth/**           — login, signup, me
///   • /api/v1/owner/**/activate — license activation endpoints
///   • /api/v1/license/**        — status polling (allows unauthenticated check)
///   • /healthz                  — Kubernetes probes
///   • /scalar/**                — local API docs
///   • /openapi/**               — OpenAPI spec endpoint
///
/// HTTP 402 Payment Required is returned when the license is absent or expired,
/// with a JSON body that the frontend can parse for redirect logic.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class LicenseValidationMiddleware(
    RequestDelegate next,
    LicenseGuardOptions options,
    ILogger<LicenseValidationMiddleware> logger)
{
    // Routes explicitly excluded from license enforcement
    private static readonly string[] BypassPrefixes =
    [
        "/api/v1/auth/",
        "/api/v1/owner/",        // activation endpoint lives here; see logic below
        "/api/v1/license/",
        "/healthz",
        "/scalar",
        "/openapi",
    ];

    // Within /api/v1/owner/ we DO enforce the license — except for activate & activation-status
    private static readonly string[] OwnerActivationSuffixes =
    [
        "/activate",
        "/activation-status",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Bypass check — skip non-API and explicitly bypassed paths
        var path = context.Request.Path.Value ?? string.Empty;

        if (ShouldBypass(path))
        {
            await next(context);
            return;
        }

        // 2. Only enforce for authenticated requests
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        // 3. Resolve owner ID from JWT
        var sub     = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? context.User.FindFirstValue("sub");

        if (!Guid.TryParse(sub, out var ownerId))
        {
            await next(context);
            return;
        }

        // 4. Fast local license check (no network)
        var db      = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var license = await db.BusinessLicenses
            .AsNoTracking()
            .Where(l => l.OwnerId == ownerId && l.Status == LicenseStatus.Active)
            .OrderByDescending(l => l.CreatedAtUtc)
            .FirstOrDefaultAsync(context.RequestAborted);

        var result = LicenseGuardService.BuildLocalResult(license, options);

        if (result.IsValid)
        {
            // Attach guard result to HttpContext.Items for downstream use (e.g. audit logging)
            context.Items["LicenseGuardResult"] = result;
            await next(context);
            return;
        }

        // 5. License invalid — return 402 with structured JSON
        logger.LogWarning(
            "[LicenseMiddleware] Blocking request to {Path} for owner {OwnerId}. Code={Code}",
            path, ownerId, result.Code);

        context.Response.StatusCode  = StatusCodes.Status402PaymentRequired;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new
        {
            success = false,
            code    = result.Code,
            message = result.Message,
            errors  = new[] { result.Message },
        });

        await context.Response.WriteAsync(body, context.RequestAborted);
    }

    // ── Path routing helpers ──────────────────────────────────────────────────

    private static bool ShouldBypass(string path)
    {
        foreach (var prefix in BypassPrefixes)
        {
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            // /api/v1/owner/** is only bypassed for activation endpoints
            if (prefix.Equals("/api/v1/owner/", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var suffix in OwnerActivationSuffixes)
                {
                    if (path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                        return true;  // bypass activate / activation-status
                }
                return false; // enforce license for all other /owner/ routes
            }

            return true;
        }

        return false;
    }
}
