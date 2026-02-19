namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Shared business-party fields for DRY modeling.
/// Used by entities such as Business and Vendor.
/// </summary>
public abstract class BusinessPartyBase
{
    public Guid Id { get; set; }

    /// <summary>
    /// Official name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Physical address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string StateProvince { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }
}
