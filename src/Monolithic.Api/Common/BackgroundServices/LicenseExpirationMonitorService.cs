using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Common.BackgroundServices;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Daily background monitor that validates all active licenses against the
/// remote GitHub license mapping.
///
/// Behaviour:
///   • Runs every <see cref="LicenseGuardOptions.MonitorIntervalHours"/> hours
///     (default: 24h — once per day).
///   • Calls <see cref="ILicenseGuardService.SweepAllAsync"/> which:
///       – Skips gracefully when there is no internet.
///       – Deletes expired / revoked licenses from the local database.
///       – Updates <c>LastRemoteValidatedAtUtc</c> on valid licenses.
///   • On reconnect (next successful sweep) re-validation runs automatically.
///
/// The service NEVER blocks HTTP requests — it runs independently of the
/// request pipeline.  The <see cref="LicenseValidationMiddleware"/> provides
/// the per-request gate and reads from the local database (fast path).
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class LicenseExpirationMonitorService(
    IServiceScopeFactory scopeFactory,
    LicenseGuardOptions options,
    ILogger<LicenseExpirationMonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "[LicenseMonitor] Started. Interval: {Hours}h, warning threshold: {Days}d.",
            options.MonitorIntervalHours,
            options.ExpiryWarningDays);

        // Initial short delay so startup log-spam is reduced
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunSweepAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(options.MonitorIntervalHours), stoppingToken);
        }
    }

    private async Task RunSweepAsync(CancellationToken ct)
    {
        logger.LogInformation("[LicenseMonitor] Sweep starting at {UtcNow:O}.", DateTimeOffset.UtcNow);
        try
        {
            using var scope   = scopeFactory.CreateScope();
            var guardService  = scope.ServiceProvider.GetRequiredService<ILicenseGuardService>();
            await guardService.SweepAllAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[LicenseMonitor] Unhandled error during sweep.");
        }
        logger.LogInformation("[LicenseMonitor] Sweep finished at {UtcNow:O}.", DateTimeOffset.UtcNow);
    }
}
