using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Common.SoftDelete;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Common.BackgroundServices;

/// <summary>
/// Hosted background service that permanently removes soft-deleted records after the
/// configured retention window has expired.
///
/// Retention priority (highest wins):
///   1. <c>BusinessSetting.SoftDeleteRetentionDays</c> — per-tenant override
///   2. <c>SoftDeletePurgeOptions.SystemDefaultRetentionDays</c> — platform-wide default
///   3. Built-in fallback: 30 days
///
/// The service runs once per day (configurable via <see cref="SoftDeletePurgeOptions"/>).
/// All hard-deletes use <see cref="ApplicationDbContext.HardDeleteWhereAsync{TEntity}"/> which
/// executes a direct <c>DELETE</c> statement, bypassing the soft-delete interception.
/// </summary>
public sealed class SoftDeletePurgeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SoftDeletePurgeService> _logger;
    private readonly SoftDeletePurgeOptions _options;

    public SoftDeletePurgeService(
        IServiceScopeFactory scopeFactory,
        ILogger<SoftDeletePurgeService> logger,
        SoftDeletePurgeOptions options)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _options      = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[SoftDeletePurge] Service started. Interval: {IntervalHours}h, system default retention: {DefaultDays}d.",
            _options.RunIntervalHours,
            _options.SystemDefaultRetentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPurgeAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[SoftDeletePurge] Error during purge run.");
            }

            await Task.Delay(TimeSpan.FromHours(_options.RunIntervalHours), stoppingToken);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private async Task RunPurgeAsync(CancellationToken ct)
    {
        _logger.LogInformation("[SoftDeletePurge] Starting purge run at {UtcNow:O}.", DateTimeOffset.UtcNow);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Build a lookup: businessId → effective retention days
        var businessRetentions = await db.BusinessSettings
            .AsNoTracking()
            .Select(s => new { s.BusinessId, RetentionDays = s.SoftDeleteRetentionDays ?? _options.SystemDefaultRetentionDays })
            .ToDictionaryAsync(x => x.BusinessId, x => x.RetentionDays, ct);

        int total = 0;

        // ── System-level entities (no BusinessId) ─────────────────────────────
        total += await PurgeSystemEntityAsync<ApplicationRole>(db, ct);
        total += await PurgeSystemEntityAsync<ApplicationUser>(db, ct);

        // ── Business-scoped entities ──────────────────────────────────────────
        // These entities have a BusinessId property, so we use the per-tenant retention.
        total += await PurgeBusinessEntityAsync<Vendor>(db, businessRetentions, ct);
        total += await PurgeBusinessEntityAsync<Customer>(db, businessRetentions, ct);

        _logger.LogInformation("[SoftDeletePurge] Purge run complete. {Total} rows permanently deleted.", total);
    }

    /// <summary>Purges system-level entities (no BusinessId) using the system default retention.</summary>
    private async Task<int> PurgeSystemEntityAsync<TEntity>(
        ApplicationDbContext db,
        CancellationToken ct)
        where TEntity : class, ISoftDeletable
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.SystemDefaultRetentionDays);
        int count = await db.HardDeleteWhereAsync<TEntity>(
            e => e.IsDeleted && e.DeletedAtUtc != null && e.DeletedAtUtc <= cutoff,
            ct);

        if (count > 0)
            _logger.LogInformation("[SoftDeletePurge] Hard-deleted {Count} {Entity} records (cutoff {Cutoff:d}).",
                count, typeof(TEntity).Name, cutoff);

        return count;
    }

    /// <summary>
    /// Purges business-scoped entities by iterating each business and applying
    /// its configured retention period.
    /// </summary>
    private async Task<int> PurgeBusinessEntityAsync<TEntity>(
        ApplicationDbContext db,
        Dictionary<Guid, int> businessRetentions,
        CancellationToken ct)
        where TEntity : class, ISoftDeletable, IBusinessScoped
    {
        int total = 0;

        foreach (var (businessId, retentionDays) in businessRetentions)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
            int count = await db.HardDeleteWhereAsync<TEntity>(
                e => e.BusinessId == businessId && e.IsDeleted && e.DeletedAtUtc != null && e.DeletedAtUtc <= cutoff,
                ct);

            if (count > 0)
                _logger.LogInformation(
                    "[SoftDeletePurge] Business {BusinessId}: hard-deleted {Count} {Entity} records (retention {Days}d, cutoff {Cutoff:d}).",
                    businessId, count, typeof(TEntity).Name, retentionDays, cutoff);

            total += count;
        }

        return total;
    }
}
