namespace Monolithic.Api.Modules.Identity.Contracts;

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Credentials used to obtain an access token.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Requests a business-context switch for the authenticated user.</summary>
public sealed record SwitchBusinessRequest(Guid BusinessId);

/// <summary>
/// Payload for self-service registration.
/// Creates a new <c>ApplicationUser</c>, assigns the default "User" role,
/// and immediately returns a JWT — user is logged in on successful signup.
/// </summary>
public sealed record SignUpRequest(
    /// <summary>User's full display name (2–120 chars).</summary>
    string FullName,
    /// <summary>Email address — must be unique in the system.</summary>
    string Email,
    /// <summary>Desired password (min 8 chars, upper, lower, digit).</summary>
    string Password,
    /// <summary>Must match <see cref="Password"/> exactly.</summary>
    string ConfirmPassword);

// ── Response DTOs ────────────────────────────────────────────────────────────

/// <summary>Returned on successful login.</summary>
public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    MeResponse User);

/// <summary>
/// Returned on successful self-service registration.
/// Carries the same JWT payload as <see cref="LoginResponse"/> so the client
/// can immediately authenticate without an extra round-trip.
/// </summary>
public sealed record SignUpResponse(
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
