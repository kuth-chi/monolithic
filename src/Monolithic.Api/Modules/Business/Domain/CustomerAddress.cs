namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// An address record owned by a customer (billing, shipping, headquarters, etc.).
/// A customer can have multiple addresses with different types.
/// </summary>
public sealed class CustomerAddress
{
    public Guid Id { get; set; }

    /// <summary>The customer this address belongs to.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Address type label, e.g. "Billing", "Shipping", "Headquarters".</summary>
    public string AddressType { get; set; } = "Billing";

    public string AddressLine1 { get; set; } = string.Empty;

    public string AddressLine2 { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string StateProvince { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Indicates this is the default address used for documents.</summary>
    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────

    public Customer Customer { get; set; } = null!;
}
