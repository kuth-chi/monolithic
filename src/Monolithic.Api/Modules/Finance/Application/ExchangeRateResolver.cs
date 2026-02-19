using Monolithic.Api.Modules.Finance.Domain;

namespace Monolithic.Api.Modules.Finance.Application;

/// <summary>
/// Resolves exchange rates for IAS 21 multi-currency translation.
///
/// Supports three translation methods:
/// <list type="bullet">
///   <item><term>Average</term>Mean rate over the reporting period (for income statement items).</item>
///   <item><term>Current</term>Closing/spot rate at the report end-date (for monetary balance sheet items).</item>
///   <item><term>Historical</term>Rate at each transaction date, falling back to current if not found.</item>
/// </list>
///
/// Rate lookup strategy for Historical mode uses a binary search over a per-currency sorted list
/// to find the most recent rate on or before the target date — no round-trip per transaction.
/// </summary>
internal sealed class ExchangeRateResolver
{
    private readonly ExchangeRateTranslationType _type;
    private readonly string _reportingCurrency;

    // fromCurrencyCode → average rate for the period
    private readonly IReadOnlyDictionary<string, decimal> _averageRates;

    // fromCurrencyCode → latest closing rate as of report ToDate
    private readonly IReadOnlyDictionary<string, decimal> _currentRates;

    // fromCurrencyCode → sorted list of (effectiveDate, rate) for historical lookups
    private readonly IReadOnlyDictionary<string, SortedList<DateOnly, decimal>> _historicalSeries;

    internal ExchangeRateResolver(
        ExchangeRateTranslationType type,
        string reportingCurrency,
        IReadOnlyDictionary<string, decimal> averageRates,
        IReadOnlyDictionary<string, decimal> currentRates,
        IReadOnlyDictionary<string, SortedList<DateOnly, decimal>> historicalSeries)
    {
        _type = type;
        _reportingCurrency = reportingCurrency.ToUpperInvariant();
        _averageRates = averageRates;
        _currentRates = currentRates;
        _historicalSeries = historicalSeries;
    }

    /// <summary>
    /// Returns the multiplier to convert 1 unit of <paramref name="fromCurrency"/> into the reporting currency.
    /// Returns 1.0 when source and reporting currency are identical.
    /// Falls back to the current closing rate if a more-specific rate cannot be resolved.
    /// </summary>
    public decimal Resolve(string fromCurrency, DateOnly transactionDate)
    {
        var from = fromCurrency.ToUpperInvariant();
        if (from == _reportingCurrency) return 1m;

        return _type switch
        {
            ExchangeRateTranslationType.Average => _averageRates.TryGetValue(from, out var avg) ? avg : GetCurrentOrFallback(from),
            ExchangeRateTranslationType.Current => GetCurrentOrFallback(from),
            ExchangeRateTranslationType.Historical => ResolveHistorical(from, transactionDate),
            _ => GetCurrentOrFallback(from)
        };
    }

    /// <summary>Exposed for report metadata building (rate summary rows).</summary>
    public IReadOnlyDictionary<string, decimal> CurrentRates => _currentRates;
    public IReadOnlyDictionary<string, decimal> AverageRates => _averageRates;

    // ── Private helpers ───────────────────────────────────────────────────────

    private decimal GetCurrentOrFallback(string from)
        => _currentRates.TryGetValue(from, out var r) ? r : 1m;

    /// <summary>
    /// Binary-searches the sorted rate series for the most recent rate on or before
    /// <paramref name="date"/>. Falls back to the current closing rate.
    /// </summary>
    private decimal ResolveHistorical(string from, DateOnly date)
    {
        if (!_historicalSeries.TryGetValue(from, out var series) || series.Count == 0)
            return GetCurrentOrFallback(from);

        var keys = series.Keys;
        int lo = 0, hi = keys.Count - 1, match = -1;

        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (keys[mid] <= date) { match = mid; lo = mid + 1; }
            else hi = mid - 1;
        }

        return match >= 0 ? series.Values[match] : GetCurrentOrFallback(from);
    }
}
