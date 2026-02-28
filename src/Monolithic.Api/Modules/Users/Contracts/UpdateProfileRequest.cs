namespace Monolithic.Api.Modules.Users.Contracts;

/// <summary>
/// Payload for <c>PUT /api/v1/users/me</c> and the admin override endpoint
/// <c>PUT /api/v1/users/{id}</c>.
///
/// Only mutable user-profile fields are accepted here.
/// Email changes are intentionally excluded (separate verified-email flow).
/// Role changes are handled by the Roles &amp; Permissions subsystem.
/// </summary>
public sealed record UpdateProfileRequest(
    /// <summary>New display name. Required; 1–120 chars.</summary>
    string FullName,

    /// <summary>New phone number, or <c>null</c> to clear.</summary>
    string? PhoneNumber
);
