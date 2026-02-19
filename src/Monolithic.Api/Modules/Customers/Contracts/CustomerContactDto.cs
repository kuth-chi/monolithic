namespace Monolithic.Api.Modules.Customers.Contracts;

public sealed class CustomerContactDto
{
    public Guid Id { get; init; }

    public Guid CustomerId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string JobTitle { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public bool IsPrimary { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }
}
