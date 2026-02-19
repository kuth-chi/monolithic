namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// ISO 4217 currency definition.
/// </summary>
public class Currency
{
    /// <summary>ISO 4217 code, e.g. "USD", "EUR", "KHR".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display symbol, e.g. "$", "€", "៛".</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Full name, e.g. "US Dollar".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Number of decimal places for this currency (0 for KHR, 2 for USD).</summary>
    public int DecimalPlaces { get; set; } = 2;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    /// <summary>Exchange rates where this currency is the source.</summary>
    public virtual ICollection<ExchangeRate> ExchangeRatesFrom { get; set; } = [];

    /// <summary>Exchange rates where this currency is the target.</summary>
    public virtual ICollection<ExchangeRate> ExchangeRatesTo { get; set; } = [];
}
