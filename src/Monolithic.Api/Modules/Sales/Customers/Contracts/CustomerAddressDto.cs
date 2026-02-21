namespace Monolithic.Api.Modules.Sales.Customers.Contracts;

public sealed class CustomerAddressDto
{
    public Guid Id { get; init; }

    public Guid CustomerId { get; init; }

    public string AddressType { get; init; } = string.Empty;

    public string AddressLine1 { get; init; } = string.Empty;

    public string AddressLine2 { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string StateProvince { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }
}
