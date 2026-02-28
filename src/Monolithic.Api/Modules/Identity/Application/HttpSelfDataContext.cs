using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Monolithic.Api.Modules.Identity.Application;

/// <summary>
/// HTTP-request-scoped implementation of <see cref="ISelfDataContext"/>.
/// Reads identity from <see cref="IHttpContextAccessor"/> — safe to inject
/// as a scoped service in the DI container.
/// </summary>
internal sealed class HttpSelfDataContext(IHttpContextAccessor httpContextAccessor) : ISelfDataContext
{
    private const string FullAccessPermission = "*:full";

    // ── ISelfDataContext ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public Guid CurrentUserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? Principal?.FindFirstValue(JwtClaimTypes.Sub);

            if (sub is null || !Guid.TryParse(sub, out var id))
                throw new InvalidOperationException(
                    "ISelfDataContext.CurrentUserId accessed outside of an authenticated request. " +
                    "Ensure the endpoint is decorated with [Authorize].");

            return id;
        }
    }

    /// <inheritdoc />
    public bool IsCurrentUser(Guid userId)
    {
        if (userId == Guid.Empty) return false;

        var sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? Principal?.FindFirstValue(JwtClaimTypes.Sub);

        return sub is not null
            && Guid.TryParse(sub, out var callerId)
            && callerId == userId;
    }

    /// <inheritdoc />
    public bool HasElevatedPermission(params string[] permissions)
    {
        var principal = Principal;
        if (principal is null) return false;

        // Super-admin bypass
        if (principal.HasClaim(c => c.Type == AppClaimTypes.Permission && c.Value == FullAccessPermission))
            return true;

        return permissions.Any(p =>
            principal.HasClaim(c => c.Type == AppClaimTypes.Permission && c.Value == p));
    }

    /// <inheritdoc />
    public bool CanAccessOwnedBy(Guid ownerId, params string[] elevatedPermissions)
        => IsCurrentUser(ownerId) || HasElevatedPermission(elevatedPermissions);

    // ── Private helpers ───────────────────────────────────────────────────────

    private ClaimsPrincipal? Principal =>
        httpContextAccessor.HttpContext?.User;

    /// <summary>
    /// Minimal JWT claim type constants used internally (avoids string literals).
    /// </summary>
    private static class JwtClaimTypes
    {
        internal const string Sub = "sub";
    }
}
