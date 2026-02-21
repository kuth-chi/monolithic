namespace Monolithic.Api.Modules.Identity.Contracts;

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Credentials used to obtain an access token.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Requests a business-context switch for the authenticated user.</summary>
public sealed record SwitchBusinessRequest(Guid BusinessId);

// ── Response DTOs ────────────────────────────────────────────────────────────

/// <summary>Returned on successful login.</summary>
public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    MeResponse User);

/// <summary>Returned after a successful business switch.</summary>
public sealed record SwitchBusinessResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    UserBusinessSummary NewDefaultBusiness);

/// <summary>Full caller profile: identity + active-business context.</summary>
public sealed record MeResponse(
    Guid UserId,
    string Email,
    string FullName,
    UserBusinessSummary? ActiveBusiness,
    IReadOnlyList<UserBusinessSummary> AllBusinesses,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

// ── Shared value objects ─────────────────────────────────────────────────────

/// <summary>Business membership summary embedded in tokens and profile responses.</summary>
public sealed record UserBusinessSummary(
    Guid BusinessId,
    string BusinessName,
    string? BusinessCode,
    bool IsDefault,
    bool IsActive);
