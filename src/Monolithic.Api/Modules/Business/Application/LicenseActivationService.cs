using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Business.Application;

// ── Interfaces ────────────────────────────────────────────────────────────────

/// <summary>
/// Fetches and caches the remote GitHub license mapping file.
/// Network is only hit once per cache window (default 5 minutes).
/// </summary>
public interface IGitHubLicenseFetchService
{
    /// <summary>
    /// Returns the parsed license mapping or null when the remote file is
    /// unreachable or malformed.
    /// </summary>
    Task<GitHubLicenseMappingRoot?> FetchAsync(CancellationToken ct = default);
}

/// <summary>
/// Validates a (user-email, businessId) pair against the remote license mapping
/// and persists the resulting <see cref="BusinessLicense"/> to the local database.
/// </summary>
public interface ILicenseActivationService
{
    /// <summary>
    /// Attempts to activate the license for <paramref name="businessId"/> by
    /// looking up the owner's email in the remote mapping.
    /// On success: upserts a <see cref="BusinessLicense"/> and returns the result.
    /// </summary>
    Task<LicenseActivationResult> ActivateAsync(Guid ownerId, Guid businessId, CancellationToken ct = default);

    /// <summary>
    /// Returns the current activation status for a business without
    /// triggering a re-sync / upsert.  Uses the remote mapping to compute
    /// <see cref="LicenseActivationStatusDto.EmailFoundInMapping"/> and
    /// <see cref="LicenseActivationStatusDto.BusinessIdFoundInMapping"/>.
    /// </summary>
    Task<LicenseActivationStatusDto> GetStatusAsync(Guid ownerId, Guid businessId, CancellationToken ct = default);
}

// ── GitHubLicenseFetchService ─────────────────────────────────────────────────

/// <summary>
/// Fetches the JSON license file from GitHub with in-memory caching.
/// </summary>
public sealed class GitHubLicenseFetchService(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<GitHubLicenseFetchService> logger) : IGitHubLicenseFetchService
{
    /// Remote URL — kept in one place so it is easy to reconfigure.
    private const string RemoteUrl =
        "https://raw.githubusercontent.com/ez-startup/.github/refs/heads/main/profile/license-mapping";

    private const string CacheKey          = "ghlic:mapping";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    /// JSON options — case-insensitive, camelCase source
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<GitHubLicenseMappingRoot?> FetchAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out GitHubLicenseMappingRoot? cached))
            return cached;

        try
        {
            var mapping = await httpClient.GetFromJsonAsync<GitHubLicenseMappingRoot>(
                RemoteUrl, JsonOpts, ct);

            if (mapping is not null)
                cache.Set(CacheKey, mapping, CacheTtl);

            return mapping;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to fetch license mapping from {Url}. Activation will be unavailable.", RemoteUrl);
            return null;
        }
    }
}

// ── LicenseActivationService ──────────────────────────────────────────────────

/// <summary>
/// Validates (email, businessId) against the remote GitHub license mapping
/// and persists a <see cref="BusinessLicense"/> when matched.
/// </summary>
public sealed class LicenseActivationService(
    ApplicationDbContext db,
    IGitHubLicenseFetchService githubService,
    ILogger<LicenseActivationService> logger) : ILicenseActivationService
{
    // ── ActivateAsync ─────────────────────────────────────────────────────────

    public async Task<LicenseActivationResult> ActivateAsync(
        Guid ownerId, Guid businessId, CancellationToken ct = default)
    {
        // 1. Resolve the owner's email from the identity store
        var ownerEmail = await ResolveOwnerEmailAsync(ownerId, ct);
        if (ownerEmail is null)
        {
            return Fail("Your session has expired. Please sign in again.", "session_expired");
        }

        // 2. Fetch remote mapping (cached)
        var mapping = await githubService.FetchAsync(ct);
        if (mapping is null)
        {
            return Fail("Unable to reach the license server. Please try again later or contact support.");
        }

        // 3. Find matching entries by email (case-insensitive)
        //    A single email can appear in multiple entries (one per registered business set),
        //    so we must search across all matching entries for the specific businessId.
        var businessIdStr = businessId.ToString().ToLowerInvariant();
        var emailEntries  = mapping.Licenses
            .Where(e => string.Equals(e.Email, ownerEmail, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (emailEntries.Count == 0)
        {
            return Fail(
                $"Your email address ({ownerEmail}) is not found in our license registry. " +
                "Please contact the SMERP team with your email and Business ID.");
        }

        // 4. Among all entries for this email, find the one that owns the requested businessId
        var entry = emailEntries.FirstOrDefault(
            e => e.BusinessIds.Any(
                id => string.Equals(id, businessIdStr, StringComparison.OrdinalIgnoreCase)));

        if (entry is null)
        {
            return Fail(
                $"Business ID {businessId} is not registered under your license. " +
                "Please verify you provided the correct Business ID to the SMERP team.");
        }

        // 5. Validate remote license status
        var remoteLicense = entry.License;
        if (!string.Equals(remoteLicense.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Fail($"Your license status is currently '{remoteLicense.Status}'. Contact support to reactivate.");
        }

        // 6. Parse plan enum (default to Professional when unrecognised)
        if (!Enum.TryParse<LicensePlan>(remoteLicense.Plan, ignoreCase: true, out var plan))
            plan = LicensePlan.Professional;

        // 7. Upsert local BusinessLicense
        var license = await UpsertLicenseAsync(ownerId, remoteLicense, plan, ct);

        var features = new Dictionary<string, bool>
        {
            ["AllowAdvancedReporting"] = remoteLicense.AllowAdvancedReporting,
            ["AllowMultiCurrency"]     = remoteLicense.AllowMultiCurrency,
            ["AllowIntegrations"]      = remoteLicense.AllowIntegrations,
        };

        logger.LogInformation(
            "License activated for owner {OwnerId} / business {BusinessId}. Plan={Plan}",
            ownerId, businessId, plan);

        return new LicenseActivationResult(
            IsActivated:            true,
            Message:                $"Congratulations! Your {plan} license is now active. Welcome to SMERP!",
            Plan:                   plan,
            MaxBusinesses:          remoteLicense.MaxBusinesses,
            MaxBranchesPerBusiness: remoteLicense.MaxBranchesPerBusiness,
            MaxEmployees:           remoteLicense.MaxEmployees,
            Features:               features,
            ExpiresOn:              remoteLicense.ExpiresOn,
            LicenseId:              license.Id);
    }

    // ── GetStatusAsync ────────────────────────────────────────────────────────

    public async Task<LicenseActivationStatusDto> GetStatusAsync(
        Guid ownerId, Guid businessId, CancellationToken ct = default)
    {
        var ownerEmail = await ResolveOwnerEmailAsync(ownerId, ct);
        var mapping    = await githubService.FetchAsync(ct);

        bool emailFound  = false;
        bool bizIdFound  = false;

        if (ownerEmail is not null && mapping is not null)
        {
            // Scan all entries for this email — multiple entries per email are supported
            // (one entry per registered business set / license tier).
            var bizIdStr     = businessId.ToString().ToLowerInvariant();
            var emailEntries = mapping.Licenses
                .Where(e => string.Equals(e.Email, ownerEmail, StringComparison.OrdinalIgnoreCase))
                .ToList();

            emailFound = emailEntries.Count > 0;
            if (emailFound)
                bizIdFound = emailEntries.Any(
                    e => e.BusinessIds.Any(
                        id => string.Equals(id, bizIdStr, StringComparison.OrdinalIgnoreCase)));
        }

        // Check local activation state
        var existingLicense = await db.BusinessLicenses
            .Where(l => l.OwnerId == ownerId && l.Status == LicenseStatus.Active)
            .OrderByDescending(l => l.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        // Business is activated when: email found, bizId found, AND local license exists
        bool isActivated = emailFound && bizIdFound && existingLicense is not null;

        LicensePlan? plan          = existingLicense?.Plan;
        LicenseStatus? licStatus   = existingLicense?.Status;

        string message = isActivated
            ? $"Your {plan} license is active."
            : emailFound && bizIdFound
                ? "License found in registry. Click 'Activate' to apply it."
                : emailFound
                    ? "Email found in registry but Business ID not yet registered. Provide your Business ID to the SMERP team."
                    : "Your license has not been activated. Please contact the SMERP team with your email and Business ID.";

        return new LicenseActivationStatusDto(
            BusinessId:               businessId,
            IsActivated:              isActivated,
            StatusMessage:            message,
            Plan:                     plan,
            LicenseStatus:            licStatus,
            EmailFoundInMapping:      emailFound,
            BusinessIdFoundInMapping: bizIdFound,
            ExpiresOn:                existingLicense?.ExpiresOn,
            LastCheckedUtc:           DateTimeOffset.UtcNow);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string?> ResolveOwnerEmailAsync(Guid ownerId, CancellationToken ct)
    {
        // ApplicationUser.Id is Guid (IdentityUser<Guid>)
        return await db.Users
            .Where(u => u.Id == ownerId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<BusinessLicense> UpsertLicenseAsync(
        Guid ownerId,
        GitHubLicenseDetail remote,
        LicensePlan plan,
        CancellationToken ct)
    {
        var existing = await db.BusinessLicenses
            .Where(l => l.OwnerId == ownerId && l.Status == LicenseStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            existing = new BusinessLicense { OwnerId = ownerId };
            db.BusinessLicenses.Add(existing);
        }
        else
        {
            existing.ModifiedAtUtc = DateTimeOffset.UtcNow;
            // Gap 3 fix: preserve TamperCount + TamperWarningMessage on re-activation.
            // An attacker cannot reset their strike counter by simply re-activating their license.
            // Only a hard-delete of the license row (done by LicenseGuardService on suspension)
            // resets the counter — which happens deliberately as part of the 3-strike penalty.
        }

        existing.Plan                   = plan;
        existing.Status                 = LicenseStatus.Active;
        existing.MaxBusinesses          = remote.MaxBusinesses;
        existing.MaxBranchesPerBusiness = remote.MaxBranchesPerBusiness;
        existing.MaxEmployees           = remote.MaxEmployees;
        existing.AllowAdvancedReporting = remote.AllowAdvancedReporting;
        existing.AllowMultiCurrency     = remote.AllowMultiCurrency;
        existing.AllowIntegrations      = remote.AllowIntegrations;
        existing.StartsOn               = remote.StartsOn;
        existing.ExpiresOn              = remote.ExpiresOn;
        existing.ExternalSubscriptionId = remote.LicenseKey;

        // Compute and store the integrity hash (used by LicenseGuardService for tamper detection)
        existing.ValidationHash            = LicenseGuardService.ComputeHash(remote.LicenseKey, ownerId, remote.StartsOn);
        existing.LastRemoteValidatedAtUtc  = DateTimeOffset.UtcNow;
        existing.ExpirationWarningIssued   = false;

        await db.SaveChangesAsync(ct);
        return existing;
    }

    private static LicenseActivationResult Fail(string message, string? errorCode = null) =>
        new(false, message, null, null, null, null, null, null, null, errorCode);
}
