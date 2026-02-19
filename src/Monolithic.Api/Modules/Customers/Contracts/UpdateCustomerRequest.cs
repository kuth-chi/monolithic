using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Customers.Contracts;

public sealed class UpdateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(50)]
    public string CustomerCode { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(20)]
    public string PhoneNumber { get; init; } = string.Empty;

    [MaxLength(50)]
    public string TaxId { get; init; } = string.Empty;

    [MaxLength(100)]
    public string PaymentTerms { get; init; } = string.Empty;

    [MaxLength(300)]
    public string Website { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Notes { get; init; } = string.Empty;

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

    public bool IsActive { get; init; } = true;
}
