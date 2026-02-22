using Monolithic.Api.Modules.Platform.Core;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Inventory;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Inventory module — stock items, variants, warehouses, stock movements,
/// and item media (images).
///
/// Dependencies: business
///
/// UI Shell : Operation (item catalog, stock levels, warehouses)
///            Admin    (none — fully operational)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class InventoryModule : ModuleBase
{
    public override string              ModuleId     => "inventory";
    public override string              DisplayName  => "Inventory";
    public override string              Version      => "1.0.0";
    public override string              Description  => "Stock items, variants, warehouses, stock movements, and item media.";
    public override string              Icon         => "cube";
    public override IEnumerable<string> Dependencies => ["business"];

    public override void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        => services.AddInventoryModule();

    public override IEnumerable<NavigationItem> GetNavigationItems()
    {
        yield return Nav("root",       "Inventory",       "/inventory",               UiContext.Operation, icon: "cube",           order: 40, isGroup: true);
        yield return Nav("items",      "Items",           "/inventory/items",         UiContext.Operation, icon: "tag",            order: 41, parentKey: "inventory.root", requiredPermissions: ["inventory:items:read"]);
        yield return Nav("warehouses", "Warehouses",      "/inventory/warehouses",    UiContext.Operation, icon: "home-modern",    order: 42, parentKey: "inventory.root", requiredPermissions: ["inventory:warehouses:read"]);
        yield return Nav("movements",  "Stock Movements", "/inventory/movements",     UiContext.Operation, icon: "arrow-path",     order: 43, parentKey: "inventory.root", requiredPermissions: ["inventory:movements:read"]);
    }

    public override IEnumerable<PermissionDescriptor> GetPermissions()
    {
        yield return Perm("items",       "read",   "View Inventory Items",    defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("items",       "write",  "Manage Inventory Items",  defaultRoles: ["admin", "manager"]);
        yield return Perm("items",       "delete", "Delete Inventory Items",  defaultRoles: ["admin"],               isSensitive: true);
        yield return Perm("warehouses",  "read",   "View Warehouses",         defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("warehouses",  "write",  "Manage Warehouses",       defaultRoles: ["admin", "manager"]);
        yield return Perm("movements",   "read",   "View Stock Movements",    defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("movements",   "write",  "Record Stock Movements",  defaultRoles: ["admin", "manager"]);
    }

    public override IEnumerable<WidgetDescriptor> GetWidgets()
    {
        yield return new WidgetDescriptor(
            WidgetKey:    PlatformConstants.LowStockWidget,
            DisplayName:  "Low Stock Alert",
            Description:  "Items that have fallen below their reorder point.",
            ModuleId:     ModuleId,
            DataEndpoint: "/api/v1/inventory/low-stock",
            DefaultColSpan: 6, DefaultRowSpan: 3, MinColSpan: 4,
            Icon: "exclamation-triangle");
    }
}
