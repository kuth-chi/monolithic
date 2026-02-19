using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Customers.Contracts;

/// <summary>
/// Shared address fields reused by both CreateCustomerAddressRequest and UpdateCustomerAddressRequest.
/// DRY base for address payloads.
/// </summary>
public abstract class CustomerAddressPayloadBase
{
    /// <summary>Address type label (e.g. "Billing", "Shipping", "Headquarters").</summary>
    [MaxLength(50)]
    public string AddressType { get; init; } = "Billing";

    [Required]
    [MaxLength(300)]
    public string AddressLine1 { get; init; } = string.Empty;

    [MaxLength(300)]
    public string AddressLine2 { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; init; } = string.Empty;

    [MaxLength(100)]
    public string StateProvince { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Country { get; init; } = string.Empty;

    [MaxLength(20)]
    public string PostalCode { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public bool IsActive { get; init; } = true;
}
