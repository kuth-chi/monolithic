using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Common.BackgroundServices;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Fake License Detective — silent background service that proactively sweeps
/// all active licenses for integrity violations every
/// <see cref="LicenseGuardOptions.TamperMonitorIntervalHours"/> hours (default: 2h).
///
/// Behaviour:
///   • Delegates to <see cref="ILicenseGuardService.SweepAllAsync"/> which:
///       – Verifies each license's SHA-256 hash against a freshly-computed one.
///       – Cross-checks (email, businessId) against the remote GitHub mapping.
///       – On hash mismatch: increments tamper-strike counter (1–2 = warning,
///         3 = hard-delete license + suspend owner account via IsActive = false).
///       – On remote revocation / expiry: hard-deletes the local row immediately.
///       – Skips gracefully when there is no internet connection.
///   • Never blocks HTTP requests — runs entirely outside the request pipeline.
///   • Uses a scoped DI scope (IServiceScopeFactory) to resolve the scoped
///     <see cref="ILicenseGuardService"/> safely from a singleton background task.
///
/// Strike thresholds (implemented in LicenseGuardService.RecordTamperStrikeAsync):
///   1st / 2nd strike — TamperWarningMessage written to BusinessLicense row;
///                       "tamper_warning" code returned in license-status API.
///   3rd strike        — license hard-deleted + ApplicationUser.IsActive = false.
///                       LicenseValidationMiddleware returns 403 for all API calls.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class LicenseTamperMonitorService(
    IServiceScopeFactory scopeFactory,
    LicenseGuardOptions options,
    ILogger<LicenseTamperMonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "[TamperDetective] Started. Scan interval: {Hours}h.",
            options.TamperMonitorIntervalHours);

        // Stagger from the daily LicenseExpirationMonitorService (which delays 30s)
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunScanAsync(stoppingToken);
            await Task.Delay(
                TimeSpan.FromHours(options.TamperMonitorIntervalHours),
                stoppingToken);
        }
    }

    private async Task RunScanAsync(CancellationToken ct)
    {
        logger.LogInformation("[TamperDetective] Scan starting at {UtcNow:O}.", DateTimeOffset.UtcNow);
        try
        {
            using var scope  = scopeFactory.CreateScope();
            var guardService = scope.ServiceProvider.GetRequiredService<ILicenseGuardService>();
            await guardService.SweepAllAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[TamperDetective] Unhandled error during scan.");
        }
        logger.LogInformation("[TamperDetective] Scan finished at {UtcNow:O}.", DateTimeOffset.UtcNow);
    }
}
