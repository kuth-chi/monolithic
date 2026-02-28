namespace Monolithic.Api.Modules.Users.Contracts;

/// <summary>
/// The canonical user-profile payload returned by the self-data endpoints
/// (<c>GET /api/v1/users/me</c> and <c>GET /api/v1/users/{id}</c>).
///
/// Extends <see cref="UserDto"/> with additional fields that are shown on
/// the profile page but not needed in list contexts.
///
/// <b>Security note:</b> This DTO is intentionally lean — it never exposes
/// password hashes, internal tokens, or security-sensitive metadata.
/// </summary>
public sealed class UserProfileDto
{
    /// <summary>Unique identifier — same as the <c>sub</c> JWT claim.</summary>
    public Guid Id { get; init; }

    /// <summary>Display name shown across the product.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Primary email — matches the <c>email</c> JWT claim.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Phone number on file (nullable).</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Whether the account is enabled and can log in.</summary>
    public bool IsActive { get; init; }

    /// <summary>When the account was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Most recent login timestamp, if any.</summary>
    public DateTimeOffset? LastLoginUtc { get; init; }

    /// <summary>Roles assigned to the user.</summary>
    public IReadOnlyCollection<string> Roles { get; init; } = [];
}
