namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Exchange rate between two currencies on a given date.
/// The rate means: 1 unit of FromCurrencyCode = Rate units of ToCurrencyCode.
/// </summary>
public class ExchangeRate
{
    public Guid Id { get; set; }

    /// <summary>Source currency ISO code (e.g. "USD").</summary>
    public string FromCurrencyCode { get; set; } = string.Empty;

    /// <summary>Target currency ISO code (e.g. "KHR").</summary>
    public string ToCurrencyCode { get; set; } = string.Empty;

    /// <summary>Rate: 1 FromCurrency = Rate ToCurrency.</summary>
    public decimal Rate { get; set; }

    /// <summary>The date from which this rate is effective.</summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>Optional: date after which this rate expires (null = still active).</summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>Source of the rate (e.g. "Manual", "NBCambodiaAPI", "OpenExchangeRates").</summary>
    public string Source { get; set; } = "Manual";

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public virtual Currency FromCurrency { get; set; } = null!;

    public virtual Currency ToCurrency { get; set; } = null!;
}
