namespace Monolithic.Api.Modules.Identity.Domain;

/// <summary>Events captured by the authentication audit logger.</summary>
public enum AuthAuditEvent
{
    LoginSuccess   = 1,
    LoginFailed    = 2,
    BusinessSwitched = 3,
    Logout         = 4
}

/// <summary>
/// Immutable audit record written on every authentication event.
/// Never updated — append-only by design.
/// </summary>
public sealed class AuthAuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Type of event that occurred.</summary>
    public AuthAuditEvent Event { get; init; }

    /// <summary>Authenticated user — null for failed logins where the email is unknown.</summary>
    public Guid? UserId { get; init; }

    /// <summary>Email provided in the login request (always captured for failed attempts).</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Active business context at the time of the event.</summary>
    public Guid? BusinessId { get; init; }

    /// <summary>Previous business — populated only for <see cref="AuthAuditEvent.BusinessSwitched"/>.</summary>
    public Guid? PreviousBusinessId { get; init; }

    /// <summary>Client IP address (IPv4 or IPv6).</summary>
    public string? IpAddress { get; init; }

    /// <summary>HTTP User-Agent header value.</summary>
    public string? UserAgent { get; init; }

    /// <summary>Whether the action succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Human-readable reason for failure — null on success.</summary>
    public string? FailureReason { get; init; }

    /// <summary>UTC timestamp — set by the logger, not the caller.</summary>
    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
