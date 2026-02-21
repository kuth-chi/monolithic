using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monolithic.Api.Modules.Finance.Application;

namespace Monolithic.Api.Modules.Finance;

public static class FinanceModuleRegistration
{
    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        // ── Bank Accounts ────────────────────────────────────────────────────
        services.AddScoped<IBankAccountService, BankAccountService>();

        // ── General Ledger — Journal Entries ─────────────────────────────────
        services.AddScoped<IJournalEntryService, JournalEntryService>();

        // ── Multi-Currency Financial Reports ─────────────────────────────────
        // IFinancialReportService: generates P&L, Balance Sheet, Trial Balance
        // with IAS 21 Average / Current / Historical currency translation.
        services.AddScoped<IFinancialReportService, FinancialReportService>();

        // IReportExportService: serialises FinancialReportDto → CSV, Excel, PDF.
        // QuestPDF Community licence is activated inside ReportExportService's
        // static constructor (called once per process on first use).
        services.AddScoped<IReportExportService, ReportExportService>();

        // ── Expenses ─────────────────────────────────────────────────────────
        services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
        services.AddScoped<IExpenseService, ExpenseService>();

        // ── TimeProvider abstraction (enables deterministic unit-testing) ─────
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
