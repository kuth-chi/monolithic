using Monolithic.Api.Modules.Platform.Core;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Purchases;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Purchases module — vendors, purchase orders, purchase returns,
/// and the procure-to-pay workflow.
///
/// Dependencies: business, inventory
///
/// UI Shell : Operation (full procure-to-pay workflow)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class PurchasesModule : ModuleBase
{
    public override string              ModuleId     => "purchases";
    public override string              DisplayName  => "Purchases";
    public override string              Version      => "1.0.0";
    public override string              Description  => "Purchase orders, purchase returns, and the full procure-to-pay workflow. Vendor management is provided by the Vendors module.";
    public override string              Icon         => "shopping-cart";
    // vendors is a required dependency — Purchases references vendor IDs on POs.
    public override IEnumerable<string> Dependencies => ["business", "inventory", "vendors"];

    public override void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        => services.AddPurchasesModule();

    public override IEnumerable<NavigationItem> GetNavigationItems()
    {
        yield return Nav("root",    "Purchases",        "/purchases",           UiContext.Operation, icon: "shopping-cart",    order: 60, isGroup: true);
        yield return Nav("rfq",     "RFQ / Estimates",  "/purchases/rfq",       UiContext.Operation, icon: "document-text",    order: 61, parentKey: "purchases.root", requiredPermissions: ["purchases:orders:read"]);
        yield return Nav("orders",  "Purchase Orders",  "/purchases/orders",    UiContext.Operation, icon: "clipboard-list",   order: 62, parentKey: "purchases.root", requiredPermissions: ["purchases:orders:read"]);
        yield return Nav("returns", "Purchase Returns", "/purchases/returns",   UiContext.Operation, icon: "arrow-uturn-left", order: 63, parentKey: "purchases.root", requiredPermissions: ["purchases:orders:read"]);
        // Vendor quick-link — navigates to the standalone Vendors module
        yield return Nav("vendors-link", "Vendors",     "/vendors",             UiContext.Operation, icon: "truck",            order: 64, parentKey: "purchases.root", requiredPermissions: ["vendors:read"]);
    }

    public override IEnumerable<PermissionDescriptor> GetPermissions()
    {
        // Vendor permissions are now declared by the Vendors module.
        yield return Perm("orders",   "read",   "View Purchase Orders",      defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("orders",   "write",  "Create / Edit POs",         defaultRoles: ["admin", "manager"]);
        yield return Perm("orders",   "approve","Approve Purchase Orders",   defaultRoles: ["admin"],              isSensitive: true);
        yield return Perm("orders",   "delete", "Cancel Purchase Orders",    defaultRoles: ["admin"],              isSensitive: true);
    }

    public override IEnumerable<WidgetDescriptor> GetWidgets()
    {
        yield return new WidgetDescriptor(
            WidgetKey:    PlatformConstants.PendingOrdersWidget,
            DisplayName:  "Pending Purchase Orders",
            Description:  "Purchase orders awaiting approval or delivery.",
            ModuleId:     ModuleId,
            DataEndpoint: "/api/v1/purchase-orders/pending",
            DefaultColSpan: 6, DefaultRowSpan: 2, MinColSpan: 4);
    }
}
