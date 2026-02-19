namespace Monolithic.Api.Modules.Finance.Domain;

/// <summary>
/// Exchange rate translation method per IAS 21 — The Effects of Changes in Foreign Exchange Rates.
/// </summary>
public enum ExchangeRateTranslationType
{
    /// <summary>
    /// Average of daily rates over the reporting period.
    /// Typically applied to income statement items (Revenue, Expenses) per IAS 21.21.
    /// </summary>
    Average = 1,

    /// <summary>
    /// Spot/closing rate at the balance sheet date.
    /// Applied to monetary items (cash, receivables, payables) per IAS 21.23.
    /// </summary>
    Current = 2,

    /// <summary>
    /// Exchange rate at the date of the original transaction.
    /// Applied to non-monetary items carried at historical cost per IAS 21.23(b).
    /// </summary>
    Historical = 3
}

/// <summary>Financial statement type requested.</summary>
public enum FinancialReportType
{
    /// <summary>Income statement showing Revenue, Cost of Goods Sold, and Expenses.</summary>
    ProfitAndLoss = 1,

    /// <summary>Statement of financial position showing Assets, Liabilities, and Equity.</summary>
    BalanceSheet = 2,

    /// <summary>Pre-adjustment listing of all account debit/credit balances.</summary>
    TrialBalance = 3
}

/// <summary>Consolidation depth for multi-entity reporting.</summary>
public enum ConsolidationLevel
{
    /// <summary>All businesses rolled up into one report (group consolidation).</summary>
    Group = 1,

    /// <summary>Individual company level.</summary>
    Company = 2,

    /// <summary>Sub-unit / division within a company.</summary>
    Division = 3
}

/// <summary>Output file format for report export.</summary>
public enum ExportFormat
{
    /// <summary>Comma-separated values, UTF-8.</summary>
    Csv = 1,

    /// <summary>Microsoft Excel (.xlsx) with formatted tables.</summary>
    Excel = 2,

    /// <summary>Portable Document Format — formatted, print-ready.</summary>
    Pdf = 3
}
