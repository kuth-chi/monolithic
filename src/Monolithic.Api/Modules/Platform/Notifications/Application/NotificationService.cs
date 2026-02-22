using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Common.Pagination;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Notifications.Contracts;
using Monolithic.Api.Modules.Platform.Notifications.Domain;
using Monolithic.Api.Modules.Platform.Templates.Application;
using Monolithic.Api.Modules.Platform.Templates.Contracts;

namespace Monolithic.Api.Modules.Platform.Notifications.Application;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Channel strategy interface. Each channel (Email, SMS, Push) implements this
/// and is keyed by <see cref="NotificationChannel"/> value.
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public interface INotificationChannel
{
    NotificationChannel Channel { get; }

    Task<(bool Success, string? Error)> SendAsync(
        string recipient, string? subject, string body, CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Orchestrates template rendering + channel routing + log persistence.
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public interface INotificationService
{
    Task<NotificationLogDto> SendAsync(SendNotificationRequest req, CancellationToken ct = default);

    Task<PagedResult<NotificationLogDto>> ListLogsAsync(NotificationListRequest req, CancellationToken ct = default);

    Task RetryFailedAsync(Guid logId, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════════════
public sealed class NotificationService(
    IPlatformDbContext db,
    ITemplateRenderService renderer,
    IEnumerable<INotificationChannel> channels,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<NotificationLogDto> SendAsync(
        SendNotificationRequest req, CancellationToken ct = default)
    {
        // ── 1. Render template (unless body is fully overridden) ──────────────
        string? subject = req.SubjectOverride;
        string body     = req.BodyOverride ?? string.Empty;

        if (req.BodyOverride is null)
        {
            var rendered = await renderer.RenderAsync(
                new RenderTemplateRequest(req.TemplateSl, req.Variables, req.BusinessId, req.UserId), ct);

            if (!rendered.Success)
            {
                logger.LogWarning("[Notification] Template render failed for {Slug}: {Error}",
                    req.TemplateSl, rendered.ErrorMessage);
                return await PersistLogAsync(req, null, null, rendered.ErrorMessage, ct);
            }

            subject = req.SubjectOverride ?? rendered.RenderedSubject;
            body    = rendered.RenderedBody ?? string.Empty;
        }

        // ── 2. Route to correct channel ──────────────────────────────────────
        var channel = channels.FirstOrDefault(c => c.Channel == req.Channel);
        if (channel is null)
        {
            var error = $"No handler registered for channel '{req.Channel}'.";
            logger.LogError("[Notification] {Error}", error);
            return await PersistLogAsync(req, subject, body, error, ct);
        }

        // ── 3. Send ───────────────────────────────────────────────────────────
        var log = await PersistLogAsync(req, subject, body, null, ct, NotificationStatus.Pending);

        var (success, error2) = await channel.SendAsync(req.Recipient, subject, body, ct);

        // ── 4. Update log ─────────────────────────────────────────────────────
        var persisted = await db.NotificationLogs.FirstAsync(l => l.Id == log.Id, ct);
        persisted.Status       = success ? NotificationStatus.Sent : NotificationStatus.Failed;
        persisted.ErrorMessage = error2;
        persisted.SentAtUtc    = success ? DateTimeOffset.UtcNow : null;
        persisted.AttemptCount++;
        await db.SaveChangesAsync(ct);

        return persisted.ToDto();
    }

    public async Task<PagedResult<NotificationLogDto>> ListLogsAsync(
        NotificationListRequest req, CancellationToken ct = default)
    {
        var query = db.NotificationLogs.AsNoTracking();
        if (req.BusinessId.HasValue) query = query.Where(l => l.BusinessId == req.BusinessId);
        if (req.UserId.HasValue)     query = query.Where(l => l.UserId == req.UserId);
        if (req.Channel.HasValue)    query = query.Where(l => l.Channel == req.Channel);
        if (req.Status.HasValue)     query = query.Where(l => l.Status == req.Status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(l => l.ToDto())
            .ToListAsync(ct);

        return new PagedResult<NotificationLogDto>
        {
            Data  = items,
            Total = total,
            Page  = req.Page,
            Size  = req.PageSize,
        };
    }

    public async Task RetryFailedAsync(Guid logId, CancellationToken ct = default)
    {
        var log = await db.NotificationLogs.FirstOrDefaultAsync(l => l.Id == logId, ct)
            ?? throw new KeyNotFoundException($"NotificationLog {logId} not found.");

        if (log.Status != NotificationStatus.Failed)
            throw new InvalidOperationException("Only failed notifications can be retried.");

        var channel = channels.FirstOrDefault(c => c.Channel == log.Channel)
            ?? throw new InvalidOperationException($"No handler for channel '{log.Channel}'.");

        var (success, error) = await channel.SendAsync(log.Recipient, log.Subject, log.Body, ct);

        log.Status       = success ? NotificationStatus.Sent : NotificationStatus.Failed;
        log.ErrorMessage = error;
        log.SentAtUtc    = success ? DateTimeOffset.UtcNow : null;
        log.AttemptCount++;
        await db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<NotificationLogDto> PersistLogAsync(
        SendNotificationRequest req,
        string? subject, string? body, string? error,
        CancellationToken ct,
        NotificationStatus status = NotificationStatus.Failed)
    {
        var log = new NotificationLog
        {
            Id          = Guid.NewGuid(),
            BusinessId  = req.BusinessId,
            UserId      = req.UserId,
            Channel     = req.Channel,
            TemplateSl  = req.TemplateSl,
            Recipient   = req.Recipient,
            Subject     = subject,
            Body        = body ?? string.Empty,
            Status      = status,
            ErrorMessage = error,
        };
        db.NotificationLogs.Add(log);
        await db.SaveChangesAsync(ct);
        return log.ToDto();
    }
}

// ── Mappers ───────────────────────────────────────────────────────────────────

file static class NotifMappers
{
    public static NotificationLogDto ToDto(this NotificationLog l) => new(
        l.Id, l.BusinessId, l.UserId, l.Channel, l.TemplateSl,
        l.Recipient, l.Subject, l.Body, l.Status, l.ErrorMessage,
        l.AttemptCount, l.SentAtUtc, l.CreatedAtUtc);
}
