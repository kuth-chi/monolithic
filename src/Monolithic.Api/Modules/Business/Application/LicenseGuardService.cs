using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Business.Application;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// License Guard Service — the runtime authority for license validity.
///
/// Responsibilities:
///   1. Local fast-path: query BusinessLicense from DB; compute DaysUntilExpiry.
///   2. Remote validation: cross-check (email, businessIds) against the GitHub
///      license mapping when called explicitly or by the background monitor.
///   3. Auto-revoke: delete (hard) the local license row when the remote
///      mapping marks it expired or invalid.
///   4. Hash verification: compare stored SHA-256 against freshly-computed hash
///      to detect in-database tampering.
///
/// Called by:
///   • GET /api/v1/license/status
///   • LicenseExpirationMonitorService (daily background sweep)
///   • LicenseValidationMiddleware       (per-request gate)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════

public interface ILicenseGuardService
{
    /// <summary>
    /// Returns the current guard result for <paramref name="ownerId"/> using the
    /// local database only (no network call).  Safe to call on every HTTP request.
    /// </summary>
    Task<LicenseGuardResult> GetLocalStatusAsync(Guid ownerId, CancellationToken ct = default);

    /// <summary>
    /// Performs a full remote validation cycle for <paramref name="ownerId"/>:
    /// fetches the GitHub mapping, verifies the license, updates
    /// <see cref="BusinessLicense.LastRemoteValidatedAtUtc"/>, and hard-deletes
    /// the local row when the remote source marks it expired or invalid.
    /// </summary>
    /// <returns>
    /// The updated <see cref="LicenseGuardResult"/> after remote check.
    /// When the network is unreachable the local result is returned with code
    /// <see cref="LicenseGuardCode.RemoteUnreachable"/>.
    /// </returns>
    Task<LicenseGuardResult> ValidateRemoteAsync(Guid ownerId, CancellationToken ct = default);

    /// <summary>
    /// Sweeps all active licenses and performs remote validation on each one.
    /// Called by <see cref="LicenseExpirationMonitorService"/> once per day.
    /// Skips silently when there is no internet.
    /// </summary>
    Task SweepAllAsync(CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

public sealed class LicenseGuardService(
    ApplicationDbContext db,
    IGitHubLicenseFetchService githubService,
    LicenseGuardOptions options,
    ILogger<LicenseGuardService> logger) : ILicenseGuardService
{
    // ── GetLocalStatusAsync ───────────────────────────────────────────────────

    public async Task<LicenseGuardResult> GetLocalStatusAsync(
        Guid ownerId, CancellationToken ct = default)
    {
        var license = await FindActiveLicenseAsync(ownerId, ct);
        return BuildLocalResult(license, options);
    }

    // ── ValidateRemoteAsync ───────────────────────────────────────────────────

    public async Task<LicenseGuardResult> ValidateRemoteAsync(
        Guid ownerId, CancellationToken ct = default)
    {
        var license = await FindActiveLicenseAsync(ownerId, ct);

        // 1. Fetch remote mapping
        var mapping = await githubService.FetchAsync(ct);
        if (mapping is null)
        {
            logger.LogWarning("[LicenseGuard] Remote mapping unreachable for owner {OwnerId}. Fallback to local.", ownerId);
            var local = BuildLocalResult(license, options);
            return local with { Code = LicenseGuardCode.RemoteUnreachable };
        }

        if (license is null)
        {
            return NoLicenseResult();
        }

        // 2. Verify hash integrity — increments strike counter instead of immediate revoke
        if (!string.IsNullOrWhiteSpace(license.ExternalSubscriptionId))
        {
            var expectedHash = ComputeHash(license.ExternalSubscriptionId, ownerId, license.StartsOn);
            if (!string.IsNullOrWhiteSpace(license.ValidationHash) &&
                !string.Equals(license.ValidationHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "[LicenseGuard] Hash mismatch for owner {OwnerId}. Strike {Count}.", ownerId, license.TamperCount + 1);
                return await RecordTamperStrikeAsync(license, ownerId, "Hash mismatch — potential database tampering.", ct);
            }
        }

        // 3. Resolve owner email
        var ownerEmail = await db.Users
            .Where(u => u.Id == ownerId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct);

        if (ownerEmail is null)
        {
            logger.LogWarning("[LicenseGuard] Owner {OwnerId} has no email. Revoking.", ownerId);
            await RevokeLicenseAsync(license, "Owner email not found.", ct);
            return RevokedResult("Owner account not found. License revoked.");
        }

        // 4. Find entry in remote mapping
        //    An email can appear in multiple entries (one per business set / license tier).
        //    Prefer the entry whose license key matches what was activated locally; fall back to first.
        var emailEntries = mapping.Licenses
            .Where(e => string.Equals(e.Email, ownerEmail, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (emailEntries.Count == 0)
        {
            logger.LogWarning("[LicenseGuard] Email {Email} not found in remote mapping. Revoking.", ownerEmail);
            await RevokeLicenseAsync(license, "Email not found in remote mapping.", ct);
            return RevokedResult("Your email was removed from the license registry. Contact support.");
        }

        // Match by stored license key so multi-entry accounts resolve to the correct tier
        var entry = (!string.IsNullOrWhiteSpace(license.ExternalSubscriptionId)
                        ? emailEntries.FirstOrDefault(e =>
                            string.Equals(e.License?.LicenseKey, license.ExternalSubscriptionId,
                                StringComparison.OrdinalIgnoreCase))
                        : null)
                    ?? emailEntries.First();

        var remote = entry.License;

        // 5. Check remote status
        if (!string.Equals(remote.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("[LicenseGuard] Remote status for {Email} is '{Status}'. Revoking.", ownerEmail, remote.Status);
            await RevokeLicenseAsync(license, $"Remote status: {remote.Status}", ct);
            return RevokedResult($"License status from registry: '{remote.Status}'. Contact support.");
        }

        // 6. Check remote expiry (source of truth)
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (remote.ExpiresOn.HasValue && remote.ExpiresOn.Value < today)
        {
            logger.LogInformation("[LicenseGuard] Remote license for {Email} expired on {ExpiresOn}. Deleting.", ownerEmail, remote.ExpiresOn);
            await RevokeLicenseAsync(license, $"Expired on {remote.ExpiresOn}", ct);
            return ExpiredResult(remote.ExpiresOn.Value);
        }

        // 7. Update remote-validation timestamp and sync quota
        license.LastRemoteValidatedAtUtc = DateTimeOffset.UtcNow;
        license.ExpiresOn                = remote.ExpiresOn;
        license.MaxBusinesses           = remote.MaxBusinesses;
        license.MaxBranchesPerBusiness  = remote.MaxBranchesPerBusiness;
        license.MaxEmployees            = remote.MaxEmployees;
        license.AllowAdvancedReporting  = remote.AllowAdvancedReporting;
        license.AllowMultiCurrency      = remote.AllowMultiCurrency;
        license.AllowIntegrations       = remote.AllowIntegrations;
        license.ModifiedAtUtc           = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[LicenseGuard] Remote validation OK for owner {OwnerId}.", ownerId);
        return BuildLocalResult(license, options);
    }

    // ── SweepAllAsync ─────────────────────────────────────────────────────────

    public async Task SweepAllAsync(CancellationToken ct = default)
    {
        // Connectivity pre-check: abort early when offline so we don't log
        // thousands of spurious warnings on isolated networks.
        var mapping = await githubService.FetchAsync(ct);
        if (mapping is null)
        {
            logger.LogInformation("[LicenseGuard] Sweep skipped — remote mapping unreachable.");
            return;
        }

        var ownerIds = await db.BusinessLicenses
            .Where(l => l.Status == LicenseStatus.Active)
            .Select(l => l.OwnerId)
            .Distinct()
            .ToListAsync(ct);

        logger.LogInformation("[LicenseGuard] Sweeping {Count} active license owners.", ownerIds.Count);

        foreach (var ownerId in ownerIds)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                await ValidateRemoteAsync(ownerId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[LicenseGuard] Error sweeping owner {OwnerId}.", ownerId);
            }
        }

        logger.LogInformation("[LicenseGuard] Sweep complete.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<BusinessLicense?> FindActiveLicenseAsync(Guid ownerId, CancellationToken ct)
        => db.BusinessLicenses
            .Where(l => l.OwnerId == ownerId && l.Status == LicenseStatus.Active)
            .OrderByDescending(l => l.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

    private async Task RevokeLicenseAsync(
        BusinessLicense license, string reason, CancellationToken ct)
    {
        // Hard-delete the row so the middleware treats it as "no license"
        // and the validation page is shown immediately.
        db.BusinessLicenses.Remove(license);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[LicenseGuard] License {Id} removed. Reason: {Reason}", license.Id, reason);
    }

    /// <summary>
    /// Increments the tamper-strike counter. On the 3rd strike the license is
    /// hard-deleted and the owner account is suspended (IsActive = false).
    /// Returns the corresponding <see cref="LicenseGuardResult"/>.
    /// </summary>
    private async Task<LicenseGuardResult> RecordTamperStrikeAsync(
        BusinessLicense license, Guid ownerId, string reason, CancellationToken ct)
    {
        const int maxStrikes = 3;

        license.TamperCount++;
        license.LastTamperDetectedAtUtc = DateTimeOffset.UtcNow;
        license.TamperWarningMessage    = reason;
        license.ModifiedAtUtc           = DateTimeOffset.UtcNow;

        if (license.TamperCount >= maxStrikes)
        {
            logger.LogWarning(
                "[LicenseGuard] Owner {OwnerId} reached {Strikes} tamper strikes. Suspending account.",
                ownerId, license.TamperCount);

            await SuspendAccountAsync(ownerId,
                $"Account suspended after {maxStrikes} license-tampering events. Contact support.", ct);

            // Hard-delete license after account suspended
            await RevokeLicenseAsync(license, $"3-strike suspension. {reason}", ct);

            return SuspendedResult(
                "Your account has been suspended due to repeated license integrity violations. Contact support.");
        }

        await db.SaveChangesAsync(ct);

        int remaining = maxStrikes - license.TamperCount;
        var warning   = $"License integrity warning ({license.TamperCount}/{maxStrikes}): {reason} " +
                        $"{remaining} warning(s) remaining before account suspension.";

        logger.LogWarning("[LicenseGuard] Tamper warning for owner {OwnerId}: {Warning}", ownerId, warning);

        return BuildLocalResult(license, options) with
        {
            Code               = LicenseGuardCode.TamperWarning,
            Message            = warning,
            TamperCount        = license.TamperCount,
            TamperWarningMessage = warning,
        };
    }

    /// <summary>
    /// Sets <c>IsActive = false</c> on the owner's <see cref="ApplicationUser"/> row.
    /// </summary>
    private async Task SuspendAccountAsync(Guid ownerId, string reason, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([ownerId], ct);
        if (user is null) return;

        user.IsActive         = false;
        user.SuspendedAtUtc   = DateTimeOffset.UtcNow;
        user.SuspendedReason  = reason;
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "[LicenseGuard] Account {OwnerId} suspended. Reason: {Reason}", ownerId, reason);
    }

    // ── Static builders ───────────────────────────────────────────────────────

    internal static LicenseGuardResult BuildLocalResult(
        BusinessLicense? license,
        LicenseGuardOptions opts)
    {
        if (license is null)
            return NoLicenseResult();

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        if (license.ExpiresOn.HasValue)
        {
            int days = license.ExpiresOn.Value.DayNumber - today.DayNumber;

            if (days < 0)
            {
                // Already expired locally — background sweep will hard-delete next cycle
                return ExpiredResult(license.ExpiresOn.Value);
            }

            if (days <= opts.ExpiryWarningDays)
            {
                return new LicenseGuardResult(
                    IsValid:                   true,
                    Code:                      LicenseGuardCode.ExpiringSoon,
                    Message:                   $"Your license expires in {days} day(s). Please renew soon.",
                    Plan:                      license.Plan,
                    ExpiresOn:                 license.ExpiresOn,
                    DaysUntilExpiry:           days,
                    IsExpiringSoon:            true,
                    LastRemoteValidatedAtUtc:  license.LastRemoteValidatedAtUtc,
                    TamperCount:               license.TamperCount,
                    TamperWarningMessage:      license.TamperWarningMessage);
            }
        }

        return new LicenseGuardResult(
            IsValid:                  true,
            Code:                     license.TamperCount > 0 ? LicenseGuardCode.TamperWarning : LicenseGuardCode.Valid,
            Message:                  license.TamperCount > 0
                                        ? license.TamperWarningMessage ?? "License integrity warning detected."
                                        : "License is valid.",
            Plan:                     license.Plan,
            ExpiresOn:                license.ExpiresOn,
            DaysUntilExpiry:          license.ExpiresOn.HasValue
                                        ? license.ExpiresOn.Value.DayNumber - today.DayNumber
                                        : null,
            IsExpiringSoon:           false,
            LastRemoteValidatedAtUtc: license.LastRemoteValidatedAtUtc,
            TamperCount:              license.TamperCount,
            TamperWarningMessage:     license.TamperWarningMessage);
    }

    private static LicenseGuardResult NoLicenseResult() => new(
        IsValid:                  false,
        Code:                     LicenseGuardCode.NoLicense,
        Message:                  "No active license found. Please activate your license.",
        Plan:                     null,
        ExpiresOn:                null,
        DaysUntilExpiry:          null,
        IsExpiringSoon:           false,
        LastRemoteValidatedAtUtc: null);

    private static LicenseGuardResult RevokedResult(string message) => new(
        IsValid:                  false,
        Code:                     LicenseGuardCode.Revoked,
        Message:                  message,
        Plan:                     null,
        ExpiresOn:                null,
        DaysUntilExpiry:          null,
        IsExpiringSoon:           false,
        LastRemoteValidatedAtUtc: null);

    private static LicenseGuardResult ExpiredResult(DateOnly expiredOn) => new(
        IsValid:                  false,
        Code:                     LicenseGuardCode.Expired,
        Message:                  $"Your license expired on {expiredOn:yyyy-MM-dd}. Please contact support to renew.",
        Plan:                     null,
        ExpiresOn:                expiredOn,
        DaysUntilExpiry:          0,
        IsExpiringSoon:           false,
        LastRemoteValidatedAtUtc: null);

    private static LicenseGuardResult SuspendedResult(string message) => new(
        IsValid:                  false,
        Code:                     LicenseGuardCode.AccountSuspended,
        Message:                  message,
        Plan:                     null,
        ExpiresOn:                null,
        DaysUntilExpiry:          null,
        IsExpiringSoon:           false,
        LastRemoteValidatedAtUtc: null);

    /// <summary>
    /// SHA-256 of (licenseKey | ownerId | startsOn) used for tamper detection.
    /// </summary>
    internal static string ComputeHash(string licenseKey, Guid ownerId, DateOnly startsOn)
    {
        var raw  = $"{licenseKey}|{ownerId:N}|{startsOn:yyyy-MM-dd}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hash);
    }
}
