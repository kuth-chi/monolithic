using System.Security.Claims;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Platform.Core.Infrastructure;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// HTTP-request-scoped implementation of <see cref="ITenantContext"/>.
///
/// Resolves the current tenant from the validated JWT token stored in
/// <see cref="IHttpContextAccessor"/>. All values are lazily parsed and
/// cached within the request lifetime.
///
/// Security (OWASP A01): BusinessId is read exclusively from the validated
/// JWT principal — never from request bodies, query strings, or headers —
/// preventing tenant-spoofing attacks.
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public sealed class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private ClaimsPrincipal? Principal
        => httpContextAccessor.HttpContext?.User;

    // ── ITenantContext ────────────────────────────────────────────────────────

    public Guid? BusinessId => ParseGuid(PlatformConstants.BusinessIdClaim);

    public Guid? UserId => ParseGuid(PlatformConstants.UserIdClaim);

    public string Locale
        => Principal?.FindFirstValue(PlatformConstants.LocaleClaim) ?? "en-US";

    public string TimezoneId
        => Principal?.FindFirstValue(PlatformConstants.TimezoneClaim) ?? "UTC";

    // ── Private helpers ───────────────────────────────────────────────────────

    private Guid? ParseGuid(string claimType)
    {
        var raw = Principal?.FindFirstValue(claimType);
        return Guid.TryParse(raw, out var value) ? value : null;
    }
}
