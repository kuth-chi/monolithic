using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Finance.Contracts;
using Monolithic.Api.Modules.Finance.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Finance.Application;

/// <summary>
/// Core financial report engine.
///
/// DRY architecture:
/// ┌──────────────────────────────────────────────────────────────────┐
/// │  GenerateAsync                                                   │
/// │    ├─ LoadBusinessesAsync          (shared)                     │
/// │    ├─ BuildResolverAsync           (shared rate loading)        │
/// │    ├─ LoadAndTranslateLinesAsync   (shared EF query pipeline)   │
/// │    ├─ AggregateByAccount          (shared LINQ aggregation)     │
/// │    └─ BuildSection (×N)            (DRY per account-type group) │
/// └──────────────────────────────────────────────────────────────────┘
///
/// Report-type differences are encapsulated solely in:
///   - Date filter (Balance Sheet = cumulative to ToDate; others = period only)
///   - AccountType filter
///   - Balance sign convention (Debit-normal vs Credit-normal)
///   - Summary totals computed from sections
/// </summary>
public sealed class FinancialReportService(ApplicationDbContext context) : IFinancialReportService
{
    // ── Public entry point ────────────────────────────────────────────────────

    public async Task<FinancialReportDto> GenerateAsync(
        FinancialReportRequest request,
        CancellationToken ct = default)
    {
        // 1. Load businesses
        var businesses = await LoadBusinessesAsync(request.BusinessIds, ct);
        var businessMap = businesses.ToDictionary(b => b.Id, b => b.BaseCurrencyCode);

        // 2. Unique non-reporting base currencies needing rate lookups
        var foreignBaseCurrencies = businessMap.Values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(c => !c.Equals(request.ReportingCurrencyCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // 3. Build rate resolver (single DB round-trip)
        var resolver = await BuildResolverAsync(
            foreignBaseCurrencies, request.ReportingCurrencyCode,
            request.FromDate, request.ToDate, request.TranslationType, ct);

        // 4. Load raw lines and apply currency translation
        var aggregated = await LoadAndTranslateLinesAsync(request, businessMap, resolver, ct);

        // 5. Filter zero-balance accounts when not explicitly requested
        if (!request.IncludeZeroBalances)
            aggregated = [.. aggregated.Where(a => a.TotalDebits != 0 || a.TotalCredits != 0)];

        // 6. Build report sections + summary totals
        var (sections, summary) = request.ReportType switch
        {
            FinancialReportType.ProfitAndLoss => BuildPnLReport(aggregated),
            FinancialReportType.BalanceSheet => BuildBalanceSheetReport(aggregated),
            FinancialReportType.TrialBalance => BuildTrialBalanceReport(aggregated),
            _ => throw new InvalidOperationException($"Unsupported report type: {request.ReportType}")
        };

        // 7. Compose exchange rate metadata
        var ratesUsed = BuildRatesSummary(
            resolver, foreignBaseCurrencies, request.ReportingCurrencyCode,
            request.TranslationType, request.ToDate);

        return new FinancialReportDto
        {
            ReportType = request.ReportType,
            ReportTitle = GetReportTitle(request.ReportType, request.ConsolidationLevel),
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            ReportingCurrencyCode = request.ReportingCurrencyCode.ToUpperInvariant(),
            TranslationType = request.TranslationType,
            ConsolidationLevel = request.ConsolidationLevel,
            BusinessNames = businesses.Select(b => b.Name).ToList(),
            Sections = sections,
            ExchangeRatesUsed = ratesUsed,

            // P&L totals
            TotalRevenue = summary.TotalRevenue,
            TotalExpenses = summary.TotalExpenses,
            NetIncome = summary.NetIncome,

            // Balance Sheet totals
            TotalAssets = summary.TotalAssets,
            TotalLiabilities = summary.TotalLiabilities,
            TotalEquity = summary.TotalEquity,
            TotalLiabilitiesAndEquity = summary.TotalLiabilitiesAndEquity,
            IsBalanced = summary.IsBalanced,

            // Trial Balance totals
            TrialBalanceTotalDebits = summary.TrialBalanceTotalDebits,
            TrialBalanceTotalCredits = summary.TrialBalanceTotalCredits,

            GeneratedAtUtc = DateTimeOffset.UtcNow
        };
    }

    // ── Step 1: Load businesses ───────────────────────────────────────────────

    private async Task<IReadOnlyList<BusinessInfo>> LoadBusinessesAsync(
        IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var businesses = await context.Businesses
            .AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .Select(b => new BusinessInfo(b.Id, b.Name, b.BaseCurrencyCode))
            .ToListAsync(ct);

        if (businesses.Count == 0)
            throw new InvalidOperationException("No businesses found for the provided IDs.");

        return businesses;
    }

    // ── Step 2: Build ExchangeRateResolver (single DB round-trip) ────────────

    private async Task<ExchangeRateResolver> BuildResolverAsync(
        IReadOnlyList<string> fromCurrencies,
        string toCurrency,
        DateOnly fromDate,
        DateOnly toDate,
        ExchangeRateTranslationType translationType,
        CancellationToken ct)
    {
        var to = toCurrency.ToUpperInvariant();
        var empty = new Dictionary<string, decimal>();
        var emptyHistory = new Dictionary<string, SortedList<DateOnly, decimal>>();

        if (fromCurrencies.Count == 0)
            return new ExchangeRateResolver(translationType, to, empty, empty, emptyHistory);

        // Load all qualifying rates in a single query (rolling 3-year buffer for historical)
        var bufferStart = fromDate.AddYears(-3);
        var allRates = await context.ExchangeRates
            .AsNoTracking()
            .Where(r => fromCurrencies.Contains(r.FromCurrencyCode)
                     && r.ToCurrencyCode == to
                     && r.EffectiveDate >= bufferStart
                     && r.EffectiveDate <= toDate)
            .Select(r => new RateRow(r.FromCurrencyCode, r.Rate, r.EffectiveDate))
            .ToListAsync(ct);

        // Current rate: latest entry per currency where effectiveDate <= toDate
        var currentRates = allRates
            .Where(r => r.EffectiveDate <= toDate)
            .GroupBy(r => r.From)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.EffectiveDate).First().Rate);

        // Average rate: arithmetic mean of rates inside the period
        var averageRates = allRates
            .Where(r => r.EffectiveDate >= fromDate && r.EffectiveDate <= toDate)
            .GroupBy(r => r.From)
            .ToDictionary(
                g => g.Key,
                g => g.Average(r => r.Rate));

        // Historical series: sorted list per currency for binary-search lookups
        var historicalSeries = allRates
            .GroupBy(r => r.From)
            .ToDictionary(
                g => g.Key,
                g => new SortedList<DateOnly, decimal>(
                    g.GroupBy(r => r.EffectiveDate)
                     .ToDictionary(d => d.Key, d => d.OrderByDescending(r => r.EffectiveDate).First().Rate)));

        return new ExchangeRateResolver(
            translationType, to, averageRates, currentRates, historicalSeries);
    }

    // ── Step 3: Load journal lines, translate, and aggregate ─────────────────

    private async Task<List<AggregatedAccount>> LoadAndTranslateLinesAsync(
        FinancialReportRequest request,
        Dictionary<Guid, string> businessMap,
        ExchangeRateResolver resolver,
        CancellationToken ct)
    {
        var isBalanceSheet = request.ReportType == FinancialReportType.BalanceSheet;

        // Account type whitelist per report type
        var includeTypes = request.ReportType switch
        {
            FinancialReportType.ProfitAndLoss => new[] { AccountType.Revenue, AccountType.Expense },
            FinancialReportType.BalanceSheet => new[] { AccountType.Asset, AccountType.Liability, AccountType.Equity },
            _ => Enum.GetValues<AccountType>()      // Trial Balance = all types
        };

        // EF query — fetch only posted entries; Balance Sheet is cumulative to ToDate
        var query = context.JournalEntryLines
            .AsNoTracking()
            .Where(l => request.BusinessIds.Contains(l.JournalEntry.BusinessId)
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && includeTypes.Contains(l.Account.AccountType));

        query = isBalanceSheet
            ? query.Where(l => l.JournalEntry.TransactionDate <= request.ToDate)
            : query.Where(l => l.JournalEntry.TransactionDate >= request.FromDate
                             && l.JournalEntry.TransactionDate <= request.ToDate);

        // Project to a flat DTO (avoids lazy-loading navigation properties)
        var raw = await query
            .Select(l => new RawLine(
                l.JournalEntry.BusinessId,
                l.JournalEntry.TransactionDate,
                l.AccountId,
                l.Account.AccountNumber,
                l.Account.Name,
                l.Account.AccountType,
                l.Account.AccountCategory,
                l.DebitAmountBase,
                l.CreditAmountBase))
            .ToListAsync(ct);

        // Apply translation in-memory, then aggregate by account
        return raw
            .GroupBy(l => new
            {
                l.AccountId,
                l.AccountNumber,
                l.AccountName,
                l.AccountType,
                l.AccountCategory
            })
            .Select(g =>
            {
                decimal totalDebitBase = 0m, totalCreditBase = 0m;
                decimal totalDebitTranslated = 0m, totalCreditTranslated = 0m;

                foreach (var line in g)
                {
                    var baseCurrency = businessMap.TryGetValue(line.BusinessId, out var bc) ? bc : "USD";
                    var rate = resolver.Resolve(baseCurrency, line.TransactionDate);

                    totalDebitBase += line.DebitAmountBase;
                    totalCreditBase += line.CreditAmountBase;
                    totalDebitTranslated += line.DebitAmountBase * rate;
                    totalCreditTranslated += line.CreditAmountBase * rate;
                }

                return new AggregatedAccount(
                    g.Key.AccountId,
                    g.Key.AccountNumber,
                    g.Key.AccountName,
                    g.Key.AccountType,
                    g.Key.AccountCategory,
                    totalDebitBase,
                    totalCreditBase,
                    totalDebitTranslated,
                    totalCreditTranslated);
            })
            .OrderBy(a => a.AccountNumber)
            .ToList();
    }

    // ── Step 4: Report builders ───────────────────────────────────────────────
    // Each builder delegates to the shared BuildSection helper, keeping report-type
    // logic isolated to sign conventions and section grouping only.

    private static (IReadOnlyList<FinancialReportSectionDto> Sections, ReportSummary Summary)
        BuildPnLReport(IEnumerable<AggregatedAccount> accounts)
    {
        var byType = accounts.ToLookup(a => a.AccountType);

        // Revenue: Credit-normal → balance = Credits − Debits
        var revenueSection = BuildSection("Revenue", AccountType.Revenue,
            byType[AccountType.Revenue],
            balanceFn: a => a.TotalCredits - a.TotalDebits,
            translatedBalanceFn: a => a.TranslatedCredits - a.TranslatedDebits);

        // COGS: a Debit-normal sub-section of Expenses
        var cogsSection = BuildSection("Cost of Goods Sold", AccountType.Expense,
            byType[AccountType.Expense].Where(a => a.AccountCategory == AccountCategory.CostOfGoodsSold),
            balanceFn: a => a.TotalDebits - a.TotalCredits,
            translatedBalanceFn: a => a.TranslatedDebits - a.TranslatedCredits);

        // Operating Expenses
        var opexSection = BuildSection("Operating Expenses", AccountType.Expense,
            byType[AccountType.Expense].Where(a => a.AccountCategory != AccountCategory.CostOfGoodsSold),
            balanceFn: a => a.TotalDebits - a.TotalCredits,
            translatedBalanceFn: a => a.TranslatedDebits - a.TranslatedCredits);

        var sections = new[] { revenueSection, cogsSection, opexSection }
            .Where(s => s.Lines.Count > 0)
            .ToList();

        var totalRevenue = revenueSection.TranslatedSectionBalance;
        var totalCogs = cogsSection.TranslatedSectionBalance;
        var totalOpex = opexSection.TranslatedSectionBalance;

        return (sections, new ReportSummary
        {
            TotalRevenue = totalRevenue,
            TotalExpenses = totalCogs + totalOpex,
            NetIncome = totalRevenue - totalCogs - totalOpex
        });
    }

    private static (IReadOnlyList<FinancialReportSectionDto> Sections, ReportSummary Summary)
        BuildBalanceSheetReport(IEnumerable<AggregatedAccount> accounts)
    {
        var byType = accounts.ToLookup(a => a.AccountType);

        // Assets: Debit-normal → balance = Debits − Credits
        var assetSection = BuildSection("Assets", AccountType.Asset,
            byType[AccountType.Asset],
            a => a.TotalDebits - a.TotalCredits,
            a => a.TranslatedDebits - a.TranslatedCredits);

        // Liabilities: Credit-normal → balance = Credits − Debits
        var liabSection = BuildSection("Liabilities", AccountType.Liability,
            byType[AccountType.Liability],
            a => a.TotalCredits - a.TotalDebits,
            a => a.TranslatedCredits - a.TranslatedDebits);

        // Equity: Credit-normal
        var equitySection = BuildSection("Equity", AccountType.Equity,
            byType[AccountType.Equity],
            a => a.TotalCredits - a.TotalDebits,
            a => a.TranslatedCredits - a.TranslatedDebits);

        var sections = new[] { assetSection, liabSection, equitySection }
            .Where(s => s.Lines.Count > 0)
            .ToList();

        var totalAssets = assetSection.TranslatedSectionBalance;
        var totalLiab = liabSection.TranslatedSectionBalance;
        var totalEquity = equitySection.TranslatedSectionBalance;
        var liabPlusEquity = totalLiab + totalEquity;

        return (sections, new ReportSummary
        {
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiab,
            TotalEquity = totalEquity,
            TotalLiabilitiesAndEquity = liabPlusEquity,
            IsBalanced = Math.Abs(totalAssets - liabPlusEquity) < 0.01m
        });
    }

    private static (IReadOnlyList<FinancialReportSectionDto> Sections, ReportSummary Summary)
        BuildTrialBalanceReport(IEnumerable<AggregatedAccount> accounts)
    {
        var typeOrder = new (AccountType Type, string Label)[]
        {
            (AccountType.Asset,     "Assets"),
            (AccountType.Liability, "Liabilities"),
            (AccountType.Equity,    "Equity"),
            (AccountType.Revenue,   "Revenue"),
            (AccountType.Expense,   "Expenses")
        };

        var byType = accounts.ToLookup(a => a.AccountType);

        // Trial Balance uses raw debit/credit columns; balance = Debits − Credits
        var sections = typeOrder
            .Select(t => BuildSection(t.Label, t.Type, byType[t.Type],
                a => a.TotalDebits - a.TotalCredits,
                a => a.TranslatedDebits - a.TranslatedCredits))
            .Where(s => s.Lines.Count > 0)
            .ToList();

        var totalDebits = sections.Sum(s => s.TranslatedSectionTotalDebits);
        var totalCredits = sections.Sum(s => s.TranslatedSectionTotalCredits);

        return (sections, new ReportSummary
        {
            TrialBalanceTotalDebits = totalDebits,
            TrialBalanceTotalCredits = totalCredits
        });
    }

    // ── Shared section builder (core DRY helper) ──────────────────────────────

    private static FinancialReportSectionDto BuildSection(
        string sectionName,
        AccountType accountType,
        IEnumerable<AggregatedAccount> accounts,
        Func<AggregatedAccount, decimal> balanceFn,
        Func<AggregatedAccount, decimal> translatedBalanceFn)
    {
        var lines = accounts
            .Select(a =>
            {
                var baseTotal = a.TotalDebits + a.TotalCredits;
                var translatedTotal = a.TranslatedDebits + a.TranslatedCredits;

                return new FinancialReportLineDto
                {
                    AccountNumber = a.AccountNumber,
                    AccountName = a.AccountName,
                    AccountType = accountType.ToString(),
                    AccountCategory = a.AccountCategory.ToString(),
                    TotalDebits = Math.Round(a.TotalDebits, 2),
                    TotalCredits = Math.Round(a.TotalCredits, 2),
                    Balance = Math.Round(balanceFn(a), 2),
                    TranslatedDebits = Math.Round(a.TranslatedDebits, 2),
                    TranslatedCredits = Math.Round(a.TranslatedCredits, 2),
                    TranslatedBalance = Math.Round(translatedBalanceFn(a), 2),
                    ExchangeRateApplied = baseTotal == 0 ? 1m
                        : Math.Round(translatedTotal / baseTotal, 6)
                };
            })
            .ToList();

        return new FinancialReportSectionDto
        {
            SectionName = sectionName,
            AccountTypeName = accountType.ToString(),
            Lines = lines,
            SectionTotalDebits = lines.Sum(l => l.TotalDebits),
            SectionTotalCredits = lines.Sum(l => l.TotalCredits),
            SectionBalance = lines.Sum(l => l.Balance),
            TranslatedSectionTotalDebits = lines.Sum(l => l.TranslatedDebits),
            TranslatedSectionTotalCredits = lines.Sum(l => l.TranslatedCredits),
            TranslatedSectionBalance = lines.Sum(l => l.TranslatedBalance)
        };
    }

    // ── Exchange rate metadata ────────────────────────────────────────────────

    private static IReadOnlyList<CurrencyExchangeSummaryDto> BuildRatesSummary(
        ExchangeRateResolver resolver,
        IReadOnlyList<string> fromCurrencies,
        string toCurrency,
        ExchangeRateTranslationType translationType,
        DateOnly asOfDate)
        => fromCurrencies
            .Select(from => new CurrencyExchangeSummaryDto(
                from.ToUpperInvariant(),
                toCurrency.ToUpperInvariant(),
                translationType == ExchangeRateTranslationType.Average
                    ? resolver.AverageRates.GetValueOrDefault(from, 1m)
                    : resolver.CurrentRates.GetValueOrDefault(from, 1m),
                translationType,
                translationType != ExchangeRateTranslationType.Average ? asOfDate : null))
            .ToList();

    private static string GetReportTitle(FinancialReportType type, ConsolidationLevel level)
        => (type, level) switch
        {
            (FinancialReportType.ProfitAndLoss, ConsolidationLevel.Group) => "Consolidated Profit & Loss Statement",
            (FinancialReportType.ProfitAndLoss, _) => "Profit & Loss Statement",
            (FinancialReportType.BalanceSheet, ConsolidationLevel.Group) => "Consolidated Balance Sheet",
            (FinancialReportType.BalanceSheet, _) => "Balance Sheet",
            (FinancialReportType.TrialBalance, _) => "Trial Balance",
            _ => "Financial Report"
        };

    // ── Private: internal value types (collocated to minimize indirection) ────

    private sealed record BusinessInfo(Guid Id, string Name, string BaseCurrencyCode);
    private sealed record RateRow(string From, decimal Rate, DateOnly EffectiveDate);

    private sealed record RawLine(
        Guid BusinessId,
        DateOnly TransactionDate,
        Guid AccountId,
        string AccountNumber,
        string AccountName,
        AccountType AccountType,
        AccountCategory AccountCategory,
        decimal DebitAmountBase,
        decimal CreditAmountBase);

    private sealed record AggregatedAccount(
        Guid AccountId,
        string AccountNumber,
        string AccountName,
        AccountType AccountType,
        AccountCategory AccountCategory,
        decimal TotalDebits,
        decimal TotalCredits,
        decimal TranslatedDebits,
        decimal TranslatedCredits);

    private sealed class ReportSummary
    {
        public decimal? TotalRevenue { get; init; }
        public decimal? TotalExpenses { get; init; }
        public decimal? NetIncome { get; init; }
        public decimal? TotalAssets { get; init; }
        public decimal? TotalLiabilities { get; init; }
        public decimal? TotalEquity { get; init; }
        public decimal? TotalLiabilitiesAndEquity { get; init; }
        public bool? IsBalanced { get; init; }
        public decimal? TrialBalanceTotalDebits { get; init; }
        public decimal? TrialBalanceTotalCredits { get; init; }
    }
}
