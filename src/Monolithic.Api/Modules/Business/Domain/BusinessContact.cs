using Monolithic.Api.Modules.Identity.Domain;

namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Junction entity: Maps a business to its contacts (many-to-many).
/// </summary>
public class BusinessContact
{
    public Guid BusinessId { get; set; }

    public Guid ContactId { get; set; }

    /// <summary>
    /// Role of the contact within the business (e.g., "Manager", "Accountant", "Sales").
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is the primary contact for the business.
    /// </summary>
    public bool IsPrimary { get; set; }

    public DateTimeOffset AddedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Navigation property to the business.
    /// </summary>
    public virtual Business Business { get; set; } = null!;

    /// <summary>
    /// Navigation property to the contact.
    /// </summary>
    public virtual Contact Contact { get; set; } = null!;
}
