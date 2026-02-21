using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Monolithic.Api.Modules.Identity.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Identity.Application;

/// <inheritdoc cref="IAuthAuditLogger"/>
public sealed class AuthAuditLogger : IAuthAuditLogger
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<AuthAuditLogger> _logger;

    public AuthAuditLogger(
        ApplicationDbContext db,
        IHttpContextAccessor httpContext,
        ILogger<AuthAuditLogger> logger)
    {
        _db = db;
        _httpContext = httpContext;
        _logger = logger;
    }

    public async Task LogLoginSuccessAsync(
        Guid userId, string email, Guid? businessId, CancellationToken ct = default)
    {
        var record = Build(AuthAuditEvent.LoginSuccess, success: true,
            userId: userId, email: email, businessId: businessId);

        await PersistAsync(record, ct);

        _logger.LogInformation(
            "[AUTH] Login succeeded | User={UserId} Email={Email} Business={BusinessId} IP={IpAddress} Agent={UserAgent}",
            userId, email, businessId, record.IpAddress, record.UserAgent);
    }

    public async Task LogLoginFailedAsync(
        string email, string reason, CancellationToken ct = default)
    {
        var record = Build(AuthAuditEvent.LoginFailed, success: false,
            email: email, failureReason: reason);

        await PersistAsync(record, ct);

        _logger.LogWarning(
            "[AUTH] Login failed | Email={Email} Reason={Reason} IP={IpAddress} Agent={UserAgent}",
            email, reason, record.IpAddress, record.UserAgent);
    }

    public async Task LogBusinessSwitchedAsync(
        Guid userId, string email,
        Guid previousBusinessId, Guid newBusinessId,
        CancellationToken ct = default)
    {
        var record = Build(AuthAuditEvent.BusinessSwitched, success: true,
            userId: userId, email: email,
            businessId: newBusinessId, previousBusinessId: previousBusinessId);

        await PersistAsync(record, ct);

        _logger.LogInformation(
            "[AUTH] Business switched | User={UserId} From={Previous} To={New} IP={IpAddress}",
            userId, previousBusinessId, newBusinessId, record.IpAddress);
    }

    public async Task LogLogoutAsync(
        Guid userId, string email, Guid? businessId, CancellationToken ct = default)
    {
        var record = Build(AuthAuditEvent.Logout, success: true,
            userId: userId, email: email, businessId: businessId);

        await PersistAsync(record, ct);

        _logger.LogInformation(
            "[AUTH] Logout | User={UserId} Email={Email} Business={BusinessId} IP={IpAddress}",
            userId, email, businessId, record.IpAddress);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AuthAuditLog Build(
        AuthAuditEvent @event,
        bool success,
        Guid? userId = null,
        string email = "",
        Guid? businessId = null,
        Guid? previousBusinessId = null,
        string? failureReason = null)
    {
        var http = _httpContext.HttpContext;
        var ip = http?.Connection.RemoteIpAddress?.ToString();
        var ua = http?.Request.Headers.UserAgent.ToString();

        // Respect X-Forwarded-For when behind a reverse proxy
        var forwardedFor = http?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            ip = forwardedFor.Split(',')[0].Trim();

        return new AuthAuditLog
        {
            Event            = @event,
            Success          = success,
            UserId           = userId,
            Email            = email,
            BusinessId       = businessId,
            PreviousBusinessId = previousBusinessId,
            IpAddress        = ip,
            UserAgent        = ua?.Length > 500 ? ua[..500] : ua,
            FailureReason    = failureReason,
            OccurredAtUtc    = DateTimeOffset.UtcNow
        };
    }

    private async Task PersistAsync(AuthAuditLog record, CancellationToken ct)
    {
        await _db.AuthAuditLogs.AddAsync(record, ct);
        await _db.SaveChangesAsync(ct);
    }
}
