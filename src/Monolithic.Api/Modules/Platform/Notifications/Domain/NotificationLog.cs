namespace Monolithic.Api.Modules.Platform.Notifications.Domain;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Immutable log record for every notification attempt.
/// Written after each send; supports audit, retry, and analytics queries.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public class NotificationLog
{
    public Guid Id { get; set; }

    // ── Context ───────────────────────────────────────────────────────────────

    public Guid? BusinessId { get; set; }
    public Guid? UserId     { get; set; }

    // ── Message ───────────────────────────────────────────────────────────────

    public NotificationChannel Channel     { get; set; }
    public string               TemplateSl  { get; set; } = string.Empty;
    public string               Recipient   { get; set; } = string.Empty;
    public string?              Subject     { get; set; }
    public string               Body        { get; set; } = string.Empty;

    // ── Delivery ──────────────────────────────────────────────────────────────

    public NotificationStatus Status       { get; set; } = NotificationStatus.Pending;
    public string?            ErrorMessage { get; set; }
    public int                AttemptCount { get; set; } = 0;
    public DateTimeOffset?    SentAtUtc    { get; set; }

    // ── Audit ─────────────────────────────────────────────────────────────────

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

// ─────────────────────────────────────────────────────────────────────────────

public enum NotificationChannel
{
    Email = 1,
    Sms   = 2,
    Push  = 3,
    InApp = 4,
}

public enum NotificationStatus
{
    Pending   = 0,
    Sent      = 1,
    Failed    = 2,
    Cancelled = 3,
}
