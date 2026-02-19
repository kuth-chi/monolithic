namespace Monolithic.Api.Modules.Vendors.Contracts;

public sealed class VendorDto
{
    public Guid Id { get; init; }

    public Guid BusinessId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string ContactPerson { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string StateProvince { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public string TaxId { get; init; } = string.Empty;

    public string PaymentTerms { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }
}
