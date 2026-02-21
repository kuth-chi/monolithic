namespace Monolithic.Api.Modules.Platform.Core.Abstractions;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Multi-tenant context for the current HTTP request.
///
/// Resolved from JWT claims or request headers via <c>TenantContext</c>
/// (scoped DI lifetime) and used throughout services to scope all queries
/// to the correct business without passing tenant IDs as method parameters.
///
/// OWASP A01 – Broken Access Control: the context is derived from the validated
/// JWT; callers MUST NOT trust user-supplied tenant IDs from request bodies.
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public interface ITenantContext
{
    /// <summary>Currently scoped business. Null for system/admin endpoints.</summary>
    Guid? BusinessId { get; }

    /// <summary>Authenticated user ID. Null for anonymous requests.</summary>
    Guid? UserId { get; }

    /// <summary>
    /// IETF BCP-47 locale derived from user preferences or Accept-Language header.
    /// Used for template rendering and number/date formatting.
    /// Defaults to <c>en-US</c>.
    /// </summary>
    string Locale { get; }

    /// <summary>IANA timezone, e.g. "Asia/Phnom_Penh". Defaults to "UTC".</summary>
    string TimezoneId { get; }

    /// <summary>
    /// Returns <c>true</c> when the current user has an active, non-expired
    /// business context. Use to guard multi-tenant operations.
    /// </summary>
    bool HasBusiness => BusinessId.HasValue;

    /// <summary>Throws <see cref="InvalidOperationException"/> when BusinessId is absent.</summary>
    Guid RequireBusinessId()
    {
        return BusinessId ?? throw new InvalidOperationException(
            "A business context is required for this operation but none is present in the current request.");
    }
}
