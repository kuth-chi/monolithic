using Monolithic.Api.Modules.Business.Domain;
using BusinessEntity = Monolithic.Api.Modules.Business.Domain.Business;

namespace Monolithic.Api.Modules.Identity.Domain;

/// <summary>
/// Represents a business contact person that inherits from ApplicationUser.
/// Enables matching contacts with businesses.
/// </summary>
public class Contact : ApplicationUser
{
    /// <summary>
    /// Primary business this contact belongs to.
    /// </summary>
    public Guid? PrimaryBusinessId { get; set; }

    /// <summary>
    /// Job title or position within the organization.
    /// </summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// Phone number for this contact.
    /// </summary>
    public new string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the primary business.
    /// </summary>
    public virtual BusinessEntity? PrimaryBusiness { get; set; }

    /// <summary>
    /// Navigation property to business contacts (junction).
    /// </summary>
    public virtual ICollection<BusinessContact> BusinessContacts { get; set; } = [];
}
