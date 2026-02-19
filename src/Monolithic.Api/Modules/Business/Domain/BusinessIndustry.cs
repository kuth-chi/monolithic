namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Junction entity: Maps a business to its industries (many-to-many).
/// </summary>
public class BusinessIndustry
{
    public Guid BusinessId { get; set; }

    public Guid IndustryId { get; set; }

    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Navigation property to the business.
    /// </summary>
    public virtual Business Business { get; set; } = null!;

    /// <summary>
    /// Navigation property to the industry.
    /// </summary>
    public virtual Industry Industry { get; set; } = null!;
}
