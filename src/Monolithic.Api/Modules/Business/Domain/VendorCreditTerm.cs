namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Named credit term template shared across vendors in a business.
/// Examples: "Net 30", "Net 60", "2/10 Net 30" (early-pay discount), "COD".
/// </summary>
public class VendorCreditTerm
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>Human-readable label, e.g. "Net 30".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Number of days after bill date before payment is due. 0 = COD.</summary>
    public int NetDays { get; set; } = 30;

    /// <summary>
    /// Early-payment discount percentage (e.g. 2 for "2/10 Net 30").
    /// 0 means no early-pay discount.
    /// </summary>
    public decimal EarlyPayDiscountPercent { get; set; } = 0m;

    /// <summary>Days within which the early-pay discount applies.</summary>
    public int EarlyPayDiscountDays { get; set; } = 0;

    /// <summary>Whether this term represents Cash-On-Delivery (no credit).</summary>
    public bool IsCod { get; set; } = false;

    public bool IsDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    public virtual ICollection<VendorProfile> VendorProfiles { get; set; } = [];
}
