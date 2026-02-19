namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Vendor classification / grade for relationship scoring and identification.
/// Supports custom labels (e.g., "Preferred", "Gold", "Restricted") or tier-based grades.
/// </summary>
public class VendorClass
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>Classification label, e.g. "Gold", "Preferred", "Local SME", "Restricted".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short code for API/filter use, e.g. "GOLD", "PREF".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Optional description / criteria for this class.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Display color (CSS hex) for UI badges, e.g. "#FFD700" for Gold.
    /// </summary>
    public string ColorHex { get; set; } = "#6B7280";

    /// <summary>Sort order for dropdowns. Lower = higher priority.</summary>
    public int SortOrder { get; set; } = 100;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    public virtual ICollection<VendorProfile> VendorProfiles { get; set; } = [];
}
