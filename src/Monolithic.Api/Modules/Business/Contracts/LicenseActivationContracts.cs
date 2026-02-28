using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Business.Contracts;

// ── License Activation Contracts ─────────────────────────────────────────────

/// <summary>
/// Result returned by POST /api/v1/owner/businesses/{businessId}/activate.
/// Indicates whether the remote GitHub license mapping validated the
/// (owner email, businessId) pair and — on success — which plan was applied.
/// </summary>
public sealed record LicenseActivationResult(
    /// <summary>True when a matching valid license was found and saved.</summary>
    bool IsActivated,
    /// <summary>Human-readable explanation of the activation outcome.</summary>
    string Message,
    /// <summary>The applied plan, if activation succeeded. Null on failure.</summary>
    LicensePlan? Plan,
    /// <summary>Max businesses allowed, if activated.</summary>
    int? MaxBusinesses,
    /// <summary>Max branches per business, if activated.</summary>
    int? MaxBranchesPerBusiness,
    /// <summary>Max employees, if activated.</summary>
    int? MaxEmployees,
    /// <summary>
    /// All feature flags as a flat dictionary — e.g.
    /// { "AllowAdvancedReporting": true, "AllowMultiCurrency": true, … }
    /// </summary>
    IReadOnlyDictionary<string, bool>? Features,
    /// <summary>License expiry. Null = perpetual.</summary>
    DateOnly? ExpiresOn,
    /// <summary>Applied license ID in the local database, when activated.</summary>
    Guid? LicenseId,
    /// <summary>
    /// Machine-readable error code for failure cases.
    /// E.g. "session_expired" when the JWT sub no longer maps to an existing user.
    /// Null on success.
    /// </summary>
    string? ErrorCode = null);

/// <summary>
/// Snapshot returned by GET /api/v1/owner/businesses/{businessId}/activation-status.
/// Allows the front-end to poll for activation without triggering a re-sync.
/// </summary>
public sealed record LicenseActivationStatusDto(
    Guid BusinessId,
    bool IsActivated,
    string StatusMessage,
    LicensePlan? Plan,
    LicenseStatus? LicenseStatus,
    /// <summary>Whether the user email is associated with any entry in the remote file.</summary>
    bool EmailFoundInMapping,
    /// <summary>Whether the businessId is listed under that email's entry.</summary>
    bool BusinessIdFoundInMapping,
    DateOnly? ExpiresOn,
    DateTimeOffset? LastCheckedUtc);
