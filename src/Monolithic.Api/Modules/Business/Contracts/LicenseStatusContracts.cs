using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Business.Contracts;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Contracts for the License Guard system — expiration monitoring, status
/// polling, and lock/unlock lifecycle.
///
/// Used by:
///   GET  /api/v1/license/status          — client polling
///   LicenseGuardService                  — in-process business logic
///   LicenseExpirationMonitorService      — background daily validation
///   LicenseValidationMiddleware           — HTTP gate
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Lightweight status DTO returned by GET /api/v1/license/status.
/// Frontend polls this every hour; shape must remain stable for existing clients.
/// </summary>
public sealed record LicenseStatusDto(
    /// <summary>Whether a valid, non-expired license exists for this owner.</summary>
    bool IsValid,

    /// <summary>Human-readable status code aligned with <see cref="LicenseGuardCode"/>.</summary>
    string Code,

    /// <summary>Human-readable explanation (localis
    /// ed on backend; never used for logic).</summary>
    string Message,

    /// <summary>Active license plan name. Null when no license.</summary>
    string? Plan,

    /// <summary>ISO-8601 date, e.g. "2027-01-01". Null = perpetual.</summary>
    string? ExpiresOn,

    /// <summary>
    /// Days remaining until expiry.  Null when perpetual or no license.
    /// Negative when already expired.
    /// </summary>
    int? DaysUntilExpiry,

    /// <summary>True when <see cref="DaysUntilExpiry"/> is between 0 and the warning threshold (90 days).</summary>
    bool IsExpiringSoon,

    /// <summary>UTC timestamp of the last successful remote validation.</summary>
    DateTimeOffset? LastRemoteValidatedAtUtc,

    /// <summary>
    /// Number of tamper-detection strikes recorded (0 = clean).
    /// Non-zero triggers a warning banner in the frontend.
    /// </summary>
    int TamperCount = 0,

    /// <summary>Warning message from the last tamper event. Null when TamperCount is 0.</summary>
    string? TamperWarningMessage = null);

/// <summary>Machine-readable codes used by the frontend to branch UI logic.</summary>
public static class LicenseGuardCode
{
    /// <summary>License is valid and not expiring soon.</summary>
    public const string Valid          = "valid";

    /// <summary>License is valid but expires within 90 days.</summary>
    public const string ExpiringSoon   = "expiring_soon";

    /// <summary>License has expired; it has been removed from the local database.</summary>
    public const string Expired        = "expired";

    /// <summary>No license found / never activated.</summary>
    public const string NoLicense      = "no_license";

    /// <summary>License was found invalid during remote validation and has been revoked.</summary>
    public const string Revoked        = "revoked";

    /// <summary>Remote check failed; local validation result is used as fallback.</summary>
    public const string RemoteUnreachable = "remote_unreachable";

    /// <summary>Tamper detected (1–2 strikes). License still active; warning surfaced to user.</summary>
    public const string TamperWarning  = "tamper_warning";

    /// <summary>Account suspended after 3 tamper strikes. No API access until admin review.</summary>
    public const string AccountSuspended = "account_suspended";
}

/// <summary>Internal result produced by <see cref="Application.ILicenseGuardService"/>.</summary>
public sealed record LicenseGuardResult(
    bool IsValid,
    string Code,
    string Message,
    LicensePlan? Plan,
    DateOnly? ExpiresOn,
    int? DaysUntilExpiry,
    bool IsExpiringSoon,
    DateTimeOffset? LastRemoteValidatedAtUtc,
    /// <summary>Number of detected tamper strikes. Zero when clean.</summary>
    int TamperCount = 0,
    /// <summary>Warning message to surface if TamperCount &gt; 0.</summary>
    string? TamperWarningMessage = null);

/// <summary>Options for the expiration-warning threshold and background-service interval.</summary>
public sealed class LicenseGuardOptions
{
    public const string SectionName = "LicenseGuard";

    /// <summary>Days before expiry at which the "expiring soon" warning banner activates. Default 90.</summary>
    public int ExpiryWarningDays { get; set; } = 90;

    /// <summary>How often the background monitor runs, in hours. Default 24 (once per day).</summary>
    public double MonitorIntervalHours { get; set; } = 24;

    /// <summary>
    /// How often the Fake-License Detective (tamper monitor) runs, in hours. Default 2.
    /// </summary>
    public double TamperMonitorIntervalHours { get; set; } = 2;

    /// <summary>
    /// Remote license mapping URL.
    /// Overrides the default GitHub URL when set in appsettings.
    /// </summary>
    public string? RemoteMappingUrl { get; set; }
}
