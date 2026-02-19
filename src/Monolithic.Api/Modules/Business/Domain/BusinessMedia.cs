namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Media type for business branding.
/// </summary>
public enum BusinessMediaType
{
    Logo = 0,
    CoverHeader = 1,
    Favicon = 2,
    Stamp = 3
}

/// <summary>
/// Stores branding assets (logo, cover header) for a business.
/// Follows same storage pattern as <see cref="Monolithic.Api.Modules.Inventory.Domain.InventoryItemImage"/>.
/// Multiple records per business+type are allowed; only IsCurrent = true is shown.
/// </summary>
public class BusinessMedia
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public BusinessMediaType MediaType { get; set; }

    /// <summary>Relative path in media storage (e.g. "businesses/{businessId}/logo.webp").</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Public URL after upload (set by storage service).</summary>
    public string? PublicUrl { get; set; }

    public string? ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    public string? OriginalFileName { get; set; }

    public string? AltText { get; set; }

    /// <summary>Only one media record per business+type should have IsCurrent = true.</summary>
    public bool IsCurrent { get; set; } = true;

    public DateTimeOffset UploadedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;
}
