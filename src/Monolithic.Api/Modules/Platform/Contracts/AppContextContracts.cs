namespace Monolithic.Api.Modules.Platform.Contracts;

// ═══════════════════════════════════════════════════════════════════════════════
// AppContext — Core Foundation OS-Like, 3-Key Validated Context Contract
//
// This single aggregate is the "handshake" between the backend platform and
// each frontend UI shell (Admin + Operation). It is computed once per request
// from the validated JWT (zero extra DB queries) and tells the frontend:
//
//   Key 1 — User    : who is authenticated
//   Key 2 — Business: which business context is active
//   Key 3 — Auth    : what roles/permissions the caller holds
//
// The navigation trees for both shells are permission-filtered server-side,
// so the frontend only ever receives items the caller is allowed to see.
// (OWASP A01: security enforcement at API layer; nav filtering is UX only)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Aggregate context returned by <c>GET /api/v1/platform/app-context</c>.
/// Provides everything the UI shells need to bootstrap in a single round-trip.
/// </summary>
public sealed record AppContextResponse(
    /// <summary>Key 1 — Authenticated user identity.</summary>
    UserContextDto User,

    /// <summary>Key 2 — Active business context. Null for system/admin-only users.</summary>
    BusinessContextDto? Business,

    /// <summary>Key 3 — Authorization context: roles, permissions, UI access flags.</summary>
    AuthContextDto Auth,

    /// <summary>Permission-filtered navigation trees for both UI shells.</summary>
    NavigationContextDto Navigation);

// ── Key 1: User ───────────────────────────────────────────────────────────────

public sealed record UserContextDto(
    Guid UserId,
    string Email,
    string FullName);

// ── Key 2: Business ───────────────────────────────────────────────────────────

/// <summary>
/// Active business from the JWT. All memberships (for business-switcher) are
/// available via <c>GET /api/v1/auth/me</c>.
/// </summary>
public sealed record BusinessContextDto(
    Guid BusinessId,
    string Name,
    string? Code,
    bool IsDefault);

// ── Key 3: Authorization ──────────────────────────────────────────────────────

public sealed record AuthContextDto(
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,

    /// <summary>True when the caller possesses the <c>*:full</c> super-admin grant.</summary>
    bool HasFullAccess,

    /// <summary>True when the caller can access the Admin UI shell.</summary>
    bool CanAccessAdmin,

    /// <summary>True when the caller can access the Operation UI shell.</summary>
    bool CanAccessOperation);

// ── Navigation ────────────────────────────────────────────────────────────────

/// <summary>
/// Both UI shell navigation trees, server-side filtered to items the caller
/// has permission to see.
/// </summary>
public sealed record NavigationContextDto(
    IReadOnlyList<NavigationNodeDto> Admin,
    IReadOnlyList<NavigationNodeDto> Operation);

/// <summary>A single node in a hierarchical navigation tree.</summary>
public sealed record NavigationNodeDto(
    string Key,
    string Label,
    string Route,
    string? Icon,
    int Order,
    bool IsGroup,
    string? Badge,
    string ModuleId,
    IReadOnlyList<NavigationNodeDto> Children);
