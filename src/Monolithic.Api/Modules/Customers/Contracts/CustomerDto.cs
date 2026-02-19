namespace Monolithic.Api.Modules.Customers.Contracts;

public sealed class CustomerDto
{
    public Guid Id { get; init; }

    public Guid BusinessId { get; init; }

    public string CustomerCode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string TaxId { get; init; } = string.Empty;

    public string PaymentTerms { get; init; } = string.Empty;

    public string Website { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string StateProvince { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }
}
