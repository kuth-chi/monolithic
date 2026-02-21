using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Purchases.Vendors.Contracts;

public sealed class CreateVendorRequest
{
    [Required]
    public Guid BusinessId { get; init; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(150)]
    public string ContactPerson { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(20)]
    public string PhoneNumber { get; init; } = string.Empty;

    [MaxLength(300)]
    public string Address { get; init; } = string.Empty;

    [MaxLength(100)]
    public string City { get; init; } = string.Empty;

    [MaxLength(100)]
    public string StateProvince { get; init; } = string.Empty;

    [MaxLength(100)]
    public string Country { get; init; } = string.Empty;

    [MaxLength(20)]
    public string PostalCode { get; init; } = string.Empty;

    [MaxLength(50)]
    public string TaxId { get; init; } = string.Empty;

    [MaxLength(100)]
    public string PaymentTerms { get; init; } = string.Empty;
}
