using System.Net;
using System.Net.Mail;
using Monolithic.Api.Modules.Platform.Notifications.Domain;

namespace Monolithic.Api.Modules.Platform.Notifications.Application.Channels;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// SMTP email channel. Uses <see cref="SmtpClient"/> for Development and can
/// be swapped for a SendGrid / AWS SES / Mailgun provider by replacing this
/// registration in <c>PlatformModuleRegistration</c>.
///
/// Configuration key: <c>Platform:Notifications:Smtp</c>
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public sealed class SmtpEmailChannel(
    IConfiguration configuration,
    ILogger<SmtpEmailChannel> logger) : INotificationChannel
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<(bool Success, string? Error)> SendAsync(
        string recipient, string? subject, string body, CancellationToken ct = default)
    {
        var smtpSection = configuration.GetSection("Platform:Notifications:Smtp");
        var host        = smtpSection["Host"] ?? "localhost";
        var port        = int.TryParse(smtpSection["Port"], out var p) ? p : 25;
        var useSsl      = bool.TryParse(smtpSection["UseSsl"], out var ssl) && ssl;
        var fromAddress = smtpSection["FromAddress"] ?? "no-reply@monolithic.local";
        var fromName    = smtpSection["FromName"]    ?? "Monolithic Platform";
        var username    = smtpSection["Username"];
        var password    = smtpSection["Password"];

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl             = useSsl,
                DeliveryMethod        = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials           = !string.IsNullOrWhiteSpace(username)
                    ? new NetworkCredential(username, password)
                    : null,
            };

            using var message = new MailMessage(
                new MailAddress(fromAddress, fromName),
                new MailAddress(recipient))
            {
                Subject    = subject ?? "(no subject)",
                Body       = body,
                IsBodyHtml = body.TrimStart().StartsWith('<'),
            };

            await client.SendMailAsync(message, ct);
            logger.LogInformation("[Email] Sent to {Recipient}: {Subject}", recipient, subject);
            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Email] Failed to send to {Recipient}", recipient);
            return (false, ex.Message);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Stub SMS channel. Logs the message for development/testing.
/// Replace with Twilio / AWS SNS / Nexmo in production by swapping this
/// registration.
///
/// Production config: <c>Platform:Notifications:Sms:Provider</c>
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public sealed class StubSmsChannel(ILogger<StubSmsChannel> logger) : INotificationChannel
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public Task<(bool Success, string? Error)> SendAsync(
        string recipient, string? subject, string body, CancellationToken ct = default)
    {
        // TODO: Replace with real SMS provider (Twilio/AWS SNS) for production.
        logger.LogInformation("[SMS STUB] To: {Recipient} | Message: {Body}", recipient, body);
        return Task.FromResult<(bool, string?)>((true, null));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Stub Web Push channel for development. In production, replace with
/// <c>WebPush</c> NuGet package + VAPID keys or a push provider like
/// OneSignal / Firebase Cloud Messaging.
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public sealed class StubPushChannel(ILogger<StubPushChannel> logger) : INotificationChannel
{
    public NotificationChannel Channel => NotificationChannel.Push;

    public Task<(bool Success, string? Error)> SendAsync(
        string recipient, string? subject, string body, CancellationToken ct = default)
    {
        logger.LogInformation("[PUSH STUB] To: {Recipient} | Title: {Subject} | Body: {Body}",
            recipient, subject, body);
        return Task.FromResult<(bool, string?)>((true, null));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// In-app notification channel. Persists to <c>NotificationLog</c> with
/// <see cref="NotificationChannel.InApp"/> and signals connected clients via
/// SignalR (optional — wire up SignalR hub separately).
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public sealed class InAppNotificationChannel(ILogger<InAppNotificationChannel> logger) : INotificationChannel
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public Task<(bool Success, string? Error)> SendAsync(
        string recipient, string? subject, string body, CancellationToken ct = default)
    {
        // The log entry created by NotificationService IS the in-app notification.
        // This channel succeeds immediately; the UI polls/subscribes to the log.
        logger.LogDebug("[InApp] Queued in-app notification for {Recipient}", recipient);
        return Task.FromResult<(bool, string?)>((true, null));
    }
}
