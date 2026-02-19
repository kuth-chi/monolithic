using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Business.Contracts;

public sealed class ChartOfAccountDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public string AccountNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public string AccountCategory { get; init; } = string.Empty;
    public Guid? ParentAccountId { get; init; }
    public string? ParentAccountName { get; init; }
    public bool IsHeaderAccount { get; init; }
    public string? CurrencyCode { get; init; }
    public bool IsActive { get; init; }
    public bool IsSystem { get; init; }
    public IReadOnlyList<ChartOfAccountDto> Children { get; init; } = [];
}

public sealed record CreateChartOfAccountRequest
{
    public Guid BusinessId { get; init; }
    public string AccountNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Domain.AccountType AccountType { get; init; }
    public Domain.AccountCategory AccountCategory { get; init; }
    public Guid? ParentAccountId { get; init; }
    public bool IsHeaderAccount { get; init; } = false;
    public string? CurrencyCode { get; init; }
}

public sealed record UpdateChartOfAccountRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Domain.AccountCategory AccountCategory { get; init; }
    public Guid? ParentAccountId { get; init; }
    public bool IsHeaderAccount { get; init; }
    public string? CurrencyCode { get; init; }
    public bool IsActive { get; init; }
}
