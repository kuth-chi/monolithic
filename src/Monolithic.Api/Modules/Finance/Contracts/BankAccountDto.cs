namespace Monolithic.Api.Modules.Finance.Contracts;

public sealed class BankAccountDto
{
    public Guid Id { get; init; }

    public string OwnerType { get; init; } = string.Empty;

    public Guid? BusinessId { get; init; }

    public Guid? VendorId { get; init; }

    public Guid? CustomerId { get; init; }

    public string AccountName { get; init; } = string.Empty;

    public string AccountNumber { get; init; } = string.Empty;

    public string BankName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string SwiftCode { get; init; } = string.Empty;

    public string RoutingNumber { get; init; } = string.Empty;

    public string CurrencyCode { get; init; } = string.Empty;

    public bool IsPrimary { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }
}
