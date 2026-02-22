using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Monolithic.Api.Common.Configuration;

namespace Monolithic.Api.Common.Security;

/// <summary>
/// Configures ASP.NET Core built-in rate limiting policies.
///
/// OWASP Top-10 (2021/2026) coverage:
///   A04 – Insecure Design   : prevents brute-force on auth and sensitive endpoints
///   A07 – Identification &amp; Authentication Failures: throttles login/register attempts
///
/// Policies:
///   "auth"    — strict: 10 req / minute (login, register, forgot-password)
///   "api"     — standard: 100 req / minute per authenticated user IP
///   "fixed"   — global fallback: 200 req / minute per IP
///
/// Usage:
///   app.UseRateLimiter();
///   // on controller/action:
///   [EnableRateLimiting("auth")]
/// </summary>
public static class RateLimitingExtensions
{
    public const string AuthPolicy    = "auth";
    public const string ApiPolicy     = "api";
    public const string FixedPolicy   = "fixed";

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opts = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(limiter =>
        {
            // ── Auth endpoints (login / register / OTP) ───────────────────────
            // Sliding window: strict limit to prevent brute-force / credential-stuffing.
            limiter.AddSlidingWindowLimiter(AuthPolicy, options =>
            {
                options.PermitLimit           = opts.AuthPermitLimit;
                options.Window                = TimeSpan.FromMinutes(opts.AuthWindowMinutes);
                options.SegmentsPerWindow     = 6;
                options.QueueProcessingOrder  = QueueProcessingOrder.OldestFirst;
                options.QueueLimit            = 0; // no queuing — reject immediately
            });

            // ── Standard API endpoints (per-user token bucket) ────────────────
            limiter.AddTokenBucketLimiter(ApiPolicy, options =>
            {
                options.TokenLimit            = opts.ApiTokenLimit;
                options.ReplenishmentPeriod   = TimeSpan.FromMinutes(1);
                options.TokensPerPeriod       = opts.ApiTokensPerPeriod;
                options.AutoReplenishment     = true;
                options.QueueProcessingOrder  = QueueProcessingOrder.OldestFirst;
                options.QueueLimit            = 5;
            });

            // ── Fixed-window global fallback ──────────────────────────────────
            limiter.AddFixedWindowLimiter(FixedPolicy, options =>
            {
                options.PermitLimit           = opts.FixedPermitLimit;
                options.Window                = TimeSpan.FromMinutes(1);
                options.QueueProcessingOrder  = QueueProcessingOrder.OldestFirst;
                options.QueueLimit            = 0;
            });

            // Respond with 429 + Retry-After header
            limiter.OnRejected = async (ctx, _) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    ctx.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }
                ctx.HttpContext.Response.ContentType = "application/problem+json";
                await ctx.HttpContext.Response.WriteAsync(
                    """{"type":"https://tools.ietf.org/html/rfc6585#section-4","title":"Too Many Requests","status":429,"detail":"Rate limit exceeded. Please slow down and try again."}""");
            };
        });

        return services;
    }
}
