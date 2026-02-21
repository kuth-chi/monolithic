namespace Monolithic.Api.Modules.Identity.Application;

/// <summary>
/// Well-known custom JWT claim type strings used throughout the system.
/// Centralised here to avoid magic strings (DRY).
/// </summary>
public static class AppClaimTypes
{
    /// <summary>Active business (tenant) id embedded in the access token.</summary>
    public const string BusinessId = "business_id";

    /// <summary>Active business display name embedded in the access token.</summary>
    public const string BusinessName = "business_name";

    /// <summary>Permission name — multiple claims of this type may be present.</summary>
    public const string Permission = "permission";
}
