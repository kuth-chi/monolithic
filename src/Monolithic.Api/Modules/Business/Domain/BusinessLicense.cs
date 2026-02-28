namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Governs how many businesses a single owner (ApplicationUser) may create,
/// feature access, and subscription lifecycle.
/// One user may have one active license at a time.
/// </summary>
public class BusinessLicense
{
    public Guid Id { get; set; }

    /// <summary>The user (owner) who holds this license.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Human-readable plan name.</summary>
    public LicensePlan Plan { get; set; } = LicensePlan.Free;

    /// <summary>Current lifecycle state.</summary>
    public LicenseStatus Status { get; set; } = LicenseStatus.Active;

    /// <summary>Maximum number of businesses this owner may create under this license.</summary>
    public int MaxBusinesses { get; set; } = 1;

    /// <summary>Maximum number of branches per business.</summary>
    public int MaxBranchesPerBusiness { get; set; } = 3;

    /// <summary>Maximum number of employees across all businesses.</summary>
    public int MaxEmployees { get; set; } = 10;

    /// <summary>Whether advanced reporting (cross-business, branch rollup) is allowed.</summary>
    public bool AllowAdvancedReporting { get; set; }

    /// <summary>Whether multi-currency is enabled.</summary>
    public bool AllowMultiCurrency { get; set; }

    /// <summary>Whether third-party integrations (API, webhooks) are allowed.</summary>
    public bool AllowIntegrations { get; set; }

    /// <summary>License start date.</summary>
    public DateOnly StartsOn { get; set; }

    /// <summary>License expiry date. Null = perpetual.</summary>
    public DateOnly? ExpiresOn { get; set; }

    /// <summary>External reference from payment provider (e.g. Stripe subscription ID).</summary>
    public string? ExternalSubscriptionId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Validation / Expiry Guard fields ──────────────────────────────────────

    /// <summary>
    /// SHA-256 hex of (LicenseKey + OwnerId + StartsOn) concatenated.
    /// Stored at activation and compared each remote validation cycle to detect tampering.
    /// </summary>
    public string? ValidationHash { get; set; }

    /// <summary>
    /// UTC timestamp of the last successful round-trip to the remote GitHub
    /// license mapping file.  Null = never validated remotely.
    /// </summary>
    public DateTimeOffset? LastRemoteValidatedAtUtc { get; set; }

    /// <summary>
    /// Set to true when the expiry warning banner has been raised.
    /// Reset to false if the license is renewed.
    /// </summary>
    public bool ExpirationWarningIssued { get; set; }

    // ── Tamper Detection (Fake License Detective) ─────────────────────────────

    /// <summary>
    /// Cumulative count of detected tampering events (hash mismatch / remote
    /// revocation attempts).  Three strikes trigger account suspension.
    /// </summary>
    public int TamperCount { get; set; }

    /// <summary>
    /// UTC timestamp of the most recent tamper-detection event. Null = none.
    /// </summary>
    public DateTimeOffset? LastTamperDetectedAtUtc { get; set; }

    /// <summary>
    /// Human-readable warning message set by tamper detection; surfaced to
    /// the frontend as a banner when TamperCount &gt; 0.
    /// </summary>
    public string? TamperWarningMessage { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    /// <summary>All businesses belonging to this owner.</summary>
    public virtual ICollection<BusinessOwnership> Ownerships { get; set; } = [];
}
