namespace Monolithic.Api.Modules.Business.Contracts;

public sealed record ExchangeRateDto(
    Guid Id,
    string FromCurrencyCode,
    string ToCurrencyCode,
    decimal Rate,
    DateOnly EffectiveDate,
    DateOnly? ExpiryDate,
    string Source,
    DateTimeOffset CreatedAtUtc
);

public sealed record CreateExchangeRateRequest
{
    public string FromCurrencyCode { get; init; } = string.Empty;
    public string ToCurrencyCode { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public DateOnly EffectiveDate { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public string Source { get; init; } = "Manual";
}

public sealed record ConvertAmountRequest
{
    public decimal Amount { get; init; }
    public string FromCurrencyCode { get; init; } = string.Empty;
    public string ToCurrencyCode { get; init; } = string.Empty;
    /// <summary>If null, uses the latest effective rate.</summary>
    public DateOnly? AsOfDate { get; init; }
}

public sealed record ConvertAmountResult(
    decimal OriginalAmount,
    string FromCurrencyCode,
    decimal ConvertedAmount,
    string ToCurrencyCode,
    decimal RateUsed,
    DateOnly EffectiveDate
);
