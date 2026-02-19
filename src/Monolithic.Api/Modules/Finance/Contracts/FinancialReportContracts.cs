using Monolithic.Api.Modules.Finance.Domain;

namespace Monolithic.Api.Modules.Finance.Contracts;

// ─── Requests ────────────────────────────────────────────────────────────────

/// <summary>
/// Input parameters for generating any type of financial report.
/// Supports multi-entity consolidation and IAS 21 currency translation.
/// </summary>
public sealed record FinancialReportRequest
{
    /// <summary>
    /// One or more business IDs to include.
    /// Pass multiple IDs for group-level consolidation.
    /// Must contain at least one entry.
    /// </summary>
    public required IReadOnlyList<Guid> BusinessIds { get; init; }

    /// <summary>Start of the reporting period (inclusive).</summary>
    public required DateOnly FromDate { get; init; }

    /// <summary>End of the reporting period (inclusive). Also used as the closing date for Balance Sheet.</summary>
    public required DateOnly ToDate { get; init; }

    /// <summary>ISO 4217 code for the output reporting currency (e.g. "USD", "EUR", "KHR").</summary>
    public required string ReportingCurrencyCode { get; init; }

    public FinancialReportType ReportType { get; init; } = FinancialReportType.TrialBalance;

    /// <summary>IAS 21 rate translation method to apply when converting foreign-currency amounts.</summary>
    public ExchangeRateTranslationType TranslationType { get; init; } = ExchangeRateTranslationType.Current;

    public ConsolidationLevel ConsolidationLevel { get; init; } = ConsolidationLevel.Company;

    /// <summary>
    /// When <c>true</c>, accounts with no transactions during the period are included with zero balances.
    /// Useful for completeness checks. Defaults to <c>false</c> for concise reports.
    /// </summary>
    public bool IncludeZeroBalances { get; init; } = false;
}

/// <summary>Wraps a report request together with the desired export format.</summary>
public sealed record ExportReportRequest
{
    public required FinancialReportRequest ReportRequest { get; init; }
    public ExportFormat Format { get; init; } = ExportFormat.Excel;
}

// ─── Output DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// A single account row within a financial report section.
/// Carries both functional (base) currency amounts and translated reporting-currency amounts.
/// </summary>
public sealed record FinancialReportLineDto
{
    public string AccountNumber { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public string AccountCategory { get; init; } = string.Empty;

    // ── Functional (base) currency totals ───────────────────────────────────
    public decimal TotalDebits { get; init; }
    public decimal TotalCredits { get; init; }

    /// <summary>
    /// Net balance in base currency. Sign convention: positive = normal balance for the account type
    /// (Assets/Expenses = debit normal; Liabilities/Equity/Revenue = credit normal).
    /// </summary>
    public decimal Balance { get; init; }

    // ── Reporting currency (translated) totals ───────────────────────────────
    public decimal TranslatedDebits { get; init; }
    public decimal TranslatedCredits { get; init; }
    public decimal TranslatedBalance { get; init; }

    /// <summary>Blended exchange rate applied to this account (translated ÷ base).</summary>
    public decimal ExchangeRateApplied { get; init; }
}

/// <summary>
/// A named group of account lines within a report (e.g. "Revenue", "Assets", "Operating Expenses").
/// </summary>
public sealed record FinancialReportSectionDto
{
    public string SectionName { get; init; } = string.Empty;
    public string AccountTypeName { get; init; } = string.Empty;
    public IReadOnlyList<FinancialReportLineDto> Lines { get; init; } = [];

    // ── Section aggregates (base currency) ──────────────────────────────────
    public decimal SectionTotalDebits { get; init; }
    public decimal SectionTotalCredits { get; init; }
    public decimal SectionBalance { get; init; }

    // ── Section aggregates (translated) ─────────────────────────────────────
    public decimal TranslatedSectionTotalDebits { get; init; }
    public decimal TranslatedSectionTotalCredits { get; init; }
    public decimal TranslatedSectionBalance { get; init; }
}

/// <summary>Summary of a single currency pair exchange rate used in the report.</summary>
public sealed record CurrencyExchangeSummaryDto(
    string FromCurrency,
    string ToCurrency,
    decimal RateUsed,
    ExchangeRateTranslationType TranslationType,
    DateOnly? RateAsOfDate
);

/// <summary>
/// Complete multi-currency financial report.
/// Contains all sections, translated totals, exchange rate metadata, and key financial ratios.
/// </summary>
public sealed record FinancialReportDto
{
    public FinancialReportType ReportType { get; init; }
    public string ReportTitle { get; init; } = string.Empty;
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public string ReportingCurrencyCode { get; init; } = string.Empty;
    public ExchangeRateTranslationType TranslationType { get; init; }
    public ConsolidationLevel ConsolidationLevel { get; init; }

    /// <summary>Names of the businesses included in this report.</summary>
    public IReadOnlyList<string> BusinessNames { get; init; } = [];

    /// <summary>Report body — ordered list of account sections.</summary>
    public IReadOnlyList<FinancialReportSectionDto> Sections { get; init; } = [];

    /// <summary>Exchange rates applied during translation (one entry per non-reporting base currency).</summary>
    public IReadOnlyList<CurrencyExchangeSummaryDto> ExchangeRatesUsed { get; init; } = [];

    // ── P&L summary totals (translated to reporting currency) ────────────────
    public decimal? TotalRevenue { get; init; }
    public decimal? TotalExpenses { get; init; }
    public decimal? NetIncome { get; init; }

    // ── Balance Sheet summary totals (translated) ────────────────────────────
    public decimal? TotalAssets { get; init; }
    public decimal? TotalLiabilities { get; init; }
    public decimal? TotalEquity { get; init; }
    public decimal? TotalLiabilitiesAndEquity { get; init; }

    /// <summary>True when TotalAssets ≈ TotalLiabilitiesAndEquity (within $0.01 rounding).</summary>
    public bool? IsBalanced { get; init; }

    // ── Trial Balance summary ────────────────────────────────────────────────
    public decimal? TrialBalanceTotalDebits { get; init; }
    public decimal? TrialBalanceTotalCredits { get; init; }

    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
