using Monolithic.Api.Modules.Platform.Core;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Finance;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Finance &amp; Accounting module — General Ledger, bank accounts,
/// financial reports (P&amp;L / Balance Sheet / Trial Balance), and expense management.
///
/// Dependencies: business (requires BusinessId for multi-tenant scoping)
///
/// UI Shell : Operation (bookkeeping, GL entries, reports, expenses)
///            Admin    (financial configuration)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class FinanceModule : ModuleBase
{
    public override string              ModuleId     => "finance";
    public override string              DisplayName  => "Finance & Accounting";
    public override string              Version      => "1.0.0";
    public override string              Description  => "General Ledger, bank accounts, financial reports (P&L, Balance Sheet, Trial Balance), and expense management.";
    public override string              Icon         => "banknotes";
    public override IEnumerable<string> Dependencies => ["business"];

    public override void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        => services.AddFinanceModule();

    public override IEnumerable<NavigationItem> GetNavigationItems()
    {
        // ── Operation UI ──────────────────────────────────────────────────────
        yield return Nav("root",          "Finance",           "/finance",                        UiContext.Operation, icon: "banknotes",         order: 30, isGroup: true);
        yield return Nav("bank-accounts", "Bank Accounts",     "/finance/bank-accounts",          UiContext.Operation, icon: "credit-card",        order: 31, parentKey: "finance.root",        requiredPermissions: ["finance:bank-accounts:read"]);
        yield return Nav("journal",       "Journal Entries",   "/finance/journal-entries",        UiContext.Operation, icon: "book-open",          order: 32, parentKey: "finance.root",        requiredPermissions: ["finance:journal-entries:read"]);
        yield return Nav("expenses",      "Expenses",          "/finance/expenses",               UiContext.Operation, icon: "receipt-percent",    order: 33, parentKey: "finance.root",        requiredPermissions: ["finance:expenses:read"]);
        yield return Nav("reports",       "Financial Reports", "/finance/reports",                UiContext.Operation, icon: "document-chart-bar", order: 34, parentKey: "finance.root",        requiredPermissions: ["finance:reports:read"]);
    }

    public override IEnumerable<PermissionDescriptor> GetPermissions()
    {
        yield return Perm("bank-accounts",    "read",   "View Bank Accounts",        defaultRoles: ["admin", "accountant"]);
        yield return Perm("bank-accounts",    "write",  "Manage Bank Accounts",      defaultRoles: ["admin", "accountant"],  isSensitive: true);
        yield return Perm("journal-entries",  "read",   "View Journal Entries",      defaultRoles: ["admin", "accountant"]);
        yield return Perm("journal-entries",  "write",  "Post Journal Entries",      defaultRoles: ["admin", "accountant"],  isSensitive: true);
        yield return Perm("journal-entries",  "delete", "Void Journal Entries",      defaultRoles: ["admin"],                isSensitive: true);
        yield return Perm("expenses",         "read",   "View Expenses",             defaultRoles: ["admin", "accountant", "manager", "staff"]);
        yield return Perm("expenses",         "write",  "Manage Expenses",           defaultRoles: ["admin", "accountant", "manager"]);
        yield return Perm("expenses",         "approve","Approve Expenses",          defaultRoles: ["admin", "manager"],     isSensitive: true);
        yield return Perm("reports",          "read",   "View Financial Reports",    defaultRoles: ["admin", "accountant", "manager"]);
        yield return Perm("reports",          "export", "Export Financial Reports",  defaultRoles: ["admin", "accountant"],  isSensitive: true);
    }

    public override IEnumerable<WidgetDescriptor> GetWidgets()
    {
        yield return new WidgetDescriptor(
            WidgetKey:    PlatformConstants.RevenueChartWidget,
            DisplayName:  "Revenue vs Expenses",
            Description:  "Monthly revenue vs expenses bar chart.",
            ModuleId:     ModuleId,
            DataEndpoint: "/api/v1/analytics/revenue-expense",
            DefaultColSpan: 8, DefaultRowSpan: 3, MinColSpan: 6);
    }
}
