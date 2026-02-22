using Monolithic.Api.Modules.Platform.Core;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Sales;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Sales module — customers, quotations, sales orders, invoices (AR),
/// and credit notes.
///
/// Dependencies: business, inventory
///
/// UI Shell : Operation (full order-to-cash workflow)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class SalesModule : ModuleBase
{
    public override string              ModuleId     => "sales";
    public override string              DisplayName  => "Sales";
    public override string              Version      => "1.0.0";
    public override string              Description  => "Customers, quotations, sales orders, invoices, and accounts receivable.";
    public override string              Icon         => "shopping-bag";
    public override IEnumerable<string> Dependencies => ["business", "inventory"];

    public override void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        => services.AddSalesModule();

    public override IEnumerable<NavigationItem> GetNavigationItems()
    {
        yield return Nav("root",       "Sales",          "/sales",                   UiContext.Operation, icon: "shopping-bag",     order: 50, isGroup: true);
        yield return Nav("customers",  "Customers",      "/sales/customers",         UiContext.Operation, icon: "users",            order: 51, parentKey: "sales.root", requiredPermissions: ["sales:customers:read"]);
        yield return Nav("quotations", "Quotations",     "/sales/quotations",        UiContext.Operation, icon: "document-text",    order: 52, parentKey: "sales.root", requiredPermissions: ["sales:quotations:read"]);
        yield return Nav("orders",     "Sales Orders",   "/sales/orders",            UiContext.Operation, icon: "clipboard-list",   order: 53, parentKey: "sales.root", requiredPermissions: ["sales:orders:read"]);
        yield return Nav("invoices",   "Invoices (AR)",  "/sales/invoices",          UiContext.Operation, icon: "document-check",   order: 54, parentKey: "sales.root", requiredPermissions: ["sales:invoices:read"]);
        yield return Nav("credits",    "Credit Notes",   "/sales/credit-notes",      UiContext.Operation, icon: "arrow-uturn-left", order: 55, parentKey: "sales.root", requiredPermissions: ["sales:invoices:read"]);
    }

    public override IEnumerable<PermissionDescriptor> GetPermissions()
    {
        yield return Perm("customers",  "read",   "View Customers",           defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("customers",  "write",  "Manage Customers",         defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("customers",  "delete", "Delete Customers",         defaultRoles: ["admin"],               isSensitive: true);
        yield return Perm("quotations", "read",   "View Quotations",          defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("quotations", "write",  "Manage Quotations",        defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("orders",     "read",   "View Sales Orders",        defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("orders",     "write",  "Process Sales Orders",     defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("orders",     "approve","Approve Sales Orders",     defaultRoles: ["admin", "manager"],    isSensitive: true);
        yield return Perm("invoices",   "read",   "View Invoices",            defaultRoles: ["admin", "accountant", "manager", "staff"]);
        yield return Perm("invoices",   "write",  "Issue / Manage Invoices",  defaultRoles: ["admin", "accountant", "manager"]);
        yield return Perm("invoices",   "delete", "Void Invoices",            defaultRoles: ["admin"],               isSensitive: true);
    }
}
