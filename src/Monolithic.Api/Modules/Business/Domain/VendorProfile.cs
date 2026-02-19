namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Extended AP profile for a Vendor — extends the base Vendor entity with
/// accounts-payable specific data: VAT, credit terms, credit limit, rating,
/// preferred payment method, and relationship metadata.
///
/// One-to-one with Vendor (VendorId is both PK and FK).
/// </summary>
public class VendorProfile
{
    /// <summary>Same as Vendor.Id (shared PK / FK).</summary>
    public Guid VendorId { get; set; }

    // ── VAT / Tax ─────────────────────────────────────────────────────────────
    /// <summary>Default VAT / GST rate (%) applied to bills from this vendor. e.g. 7.0 for 7%.</summary>
    public decimal DefaultVatPercent { get; set; } = 0m;

    /// <summary>Vendor's VAT registration number (separate from TaxId for companies).</summary>
    public string VatRegistrationNumber { get; set; } = string.Empty;

    /// <summary>Whether this vendor is VAT-registered (enables VAT claim).</summary>
    public bool IsVatRegistered { get; set; } = false;

    // ── Credit Terms ──────────────────────────────────────────────────────────
    /// <summary>FK to the named credit term template (e.g. "Net 30").</summary>
    public Guid? CreditTermId { get; set; }

    /// <summary>
    /// Override net days (when null, uses CreditTerm.NetDays).
    /// Allows per-vendor customization without creating a new term.
    /// </summary>
    public int? CreditTermDaysOverride { get; set; }

    /// <summary>Maximum credit amount allowed in base currency. Null = unlimited.</summary>
    public decimal? CreditLimitBase { get; set; }

    // ── Payment Preferences ───────────────────────────────────────────────────
    /// <summary>Preferred payment method: "BankTransfer", "Cheque", "Cash", "CreditCard".</summary>
    public string PreferredPaymentMethod { get; set; } = "BankTransfer";

    /// <summary>Vendor's preferred bank account ID for receiving payments.</summary>
    public Guid? PreferredBankAccountId { get; set; }

    /// <summary>Minimum payment amount before a payment run is triggered. Null = no minimum.</summary>
    public decimal? MinimumPaymentAmount { get; set; }

    // ── Classification ────────────────────────────────────────────────────────
    /// <summary>FK to VendorClass (Gold / Silver / Preferred, etc.).</summary>
    public Guid? VendorClassId { get; set; }

    /// <summary>
    /// Numeric performance rating [0–5] based on delivery, quality, price compliance.
    /// Displayed as stars. Updated manually or by automated scoring.
    /// </summary>
    public decimal PerformanceRating { get; set; } = 0m;

    /// <summary>Free-text notes on vendor performance / relationship history.</summary>
    public string RelationshipNotes { get; set; } = string.Empty;

    // ── Flags ─────────────────────────────────────────────────────────────────
    /// <summary>Vendor is on a payment hold — blocks new payment sessions.</summary>
    public bool IsOnHold { get; set; } = false;

    /// <summary>Reason for hold (shown to AP manager).</summary>
    public string HoldReason { get; set; } = string.Empty;

    /// <summary>Vendor is blacklisted — blocks new POs and bills.</summary>
    public bool IsBlacklisted { get; set; } = false;

    public string BlacklistReason { get; set; } = string.Empty;

    // ── Timestamps ────────────────────────────────────────────────────────────
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Vendor Vendor { get; set; } = null!;

    public virtual VendorCreditTerm? CreditTerm { get; set; }

    public virtual VendorClass? VendorClass { get; set; }

    public virtual VendorBankAccount? PreferredBankAccount { get; set; }
}
