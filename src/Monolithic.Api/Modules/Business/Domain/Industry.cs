namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Represents an industry classification (e.g., Retail, Manufacturing, Services).
/// A business may operate in multiple industries.
/// </summary>
public class Industry
{
    public Guid Id { get; set; }

    /// <summary>
    /// Industry name (e.g., "Retail Trade", "Software Development").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Industry code or classification (e.g., NAICS code).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the industry.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Navigation property to businesses in this industry.
    /// </summary>
    public virtual ICollection<BusinessIndustry> BusinessIndustries { get; set; } = [];
}
