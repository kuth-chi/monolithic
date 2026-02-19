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

    // ── Navigation ────────────────────────────────────────────────────────────

    /// <summary>All businesses belonging to this owner.</summary>
    public virtual ICollection<BusinessOwnership> Ownerships { get; set; } = [];
}
