using Microsoft.Extensions.Options;

namespace Monolithic.Api.Common.Security;

/// <summary>
/// Injects security-hardening HTTP response headers on every response.
///
/// OWASP Top-10 (2021/2026) coverage:
///   A02 – Cryptographic Failures  : HSTS (enforced in production via appsettings)
///   A03 – Injection                : Content-Security-Policy blocks XSS injection vectors
///   A05 – Security Misconfiguration: removes Server/X-Powered-By; sets sane defaults
///   A06 – Vulnerable Components    : Permissions-Policy restricts dangerous browser APIs
///
/// Integration:
///   app.UseSecurityHeaders();  // before UseAuthentication
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // ── Anti-clickjacking ─────────────────────────────────────────────────
        // OWASP A05 — prevents the app being framed by a third-party site.
        headers["X-Frame-Options"] = "DENY";

        // ── MIME-type sniffing prevention ──────────────────────────────────────
        // OWASP A03 — prevents browsers from misinterpreting the Content-Type.
        headers["X-Content-Type-Options"] = "nosniff";

        // ── Cross-site scripting protection (legacy browsers) ─────────────────
        headers["X-XSS-Protection"] = "1; mode=block";

        // ── Referrer policy ───────────────────────────────────────────────────
        // Do not leak the full URL to third-party sites.
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // ── Permissions / Feature policy ──────────────────────────────────────
        // Disable browser features not needed by this API.
        headers["Permissions-Policy"] =
            "geolocation=(), camera=(), microphone=(), payment=(), usb=(), " +
            "accelerometer=(), gyroscope=(), magnetometer=()";

        // ── Content-Security-Policy ───────────────────────────────────────────
        // OWASP A03 — strict CSP for API responses (no inline scripts/styles
        // since this is a JSON API; only the Scalar UI in dev needs relaxation).
        if (options.EnableStrictCsp)
        {
            headers["Content-Security-Policy"] =
                "default-src 'none'; " +
                "frame-ancestors 'none'; " +
                "form-action 'none'";
        }

        // ── HSTS ──────────────────────────────────────────────────────────────
        // OWASP A02 — only set in production over HTTPS.
        if (options.EnableHsts && context.Request.IsHttps)
        {
            // max-age = 1 year; includeSubDomains; do NOT preload unless you're sure
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // ── Remove information-disclosure headers ─────────────────────────────
        // OWASP A05 — attackers use Server / X-Powered-By for fingerprinting.
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetMvc-Version");

        await next(context);
    }
}

/// <summary>Configuration for <see cref="SecurityHeadersMiddleware"/>.</summary>
public sealed class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    /// <summary>Set to false in Development to allow Scalar UI to load its CDN assets.</summary>
    public bool EnableStrictCsp { get; init; } = true;

    /// <summary>Set to true in Production only (requires TLS).</summary>
    public bool EnableHsts { get; init; } = false;
}

/// <summary>IApplicationBuilder extension.</summary>
public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Adds <see cref="SecurityHeadersMiddleware"/> to the pipeline.
    /// Call before <c>UseAuthentication</c> and <c>MapControllers</c>.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>(
               app.ApplicationServices
                  .GetRequiredService<IOptions<SecurityHeadersOptions>>()
                  .Value);
}
