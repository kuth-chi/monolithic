namespace Monolithic.Api.Modules.Business.Contracts;

public sealed record CurrencyDto(
    string Code,
    string Symbol,
    string Name,
    int DecimalPlaces,
    bool IsActive
);

public sealed record UpsertCurrencyRequest
{
    public string Code { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int DecimalPlaces { get; init; } = 2;
    public bool IsActive { get; init; } = true;
}
