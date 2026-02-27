using Monolithic.Api.Modules.Platform.Core;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Vendors;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Vendors module — independent vendor / supplier management for all modules
/// that participate in the procure-to-pay workflow.
///
/// Vendors are a reusable business-party concept (extends BusinessPartyBase).
/// They are consumed by Purchases (POs), Finance (AP, bills, payment schedules)
/// and can be extended in future for project procurement, contracts, etc.
///
/// Dependencies: business
///
/// UI Shell : Admin     (vendor class configuration, credit terms setup)
///            Operation (vendor CRUD, AP profile, bank accounts, classification)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class VendorsModule : ModuleBase
{
    public override string              ModuleId     => "vendors";
    public override string              DisplayName  => "Vendors";
    public override string              Version      => "1.0.0";
    public override string              Description  => "Vendor / supplier management with AP profiles, credit terms, bank accounts, classification and Finance integration.";
    public override string              Icon         => "truck";
    public override IEnumerable<string> Dependencies => ["business"];

    public override void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        => services.AddVendorsModule();

    public override IEnumerable<NavigationItem> GetNavigationItems()
    {
        // ── Admin UI — vendor class and credit-term configuration ─────────────
        yield return Nav("admin-root",        "Vendors",            "/admin/vendors",                     UiContext.Admin, icon: "truck",              order: 55, isGroup: true);
        yield return Nav("vendor-classes",    "Vendor Classes",     "/admin/vendors/classes",             UiContext.Admin, icon: "tag",                order: 56, parentKey: "vendors.admin-root", requiredPermissions: ["vendors:classes:write"]);
        yield return Nav("credit-terms",      "Credit Terms",       "/admin/vendors/credit-terms",        UiContext.Admin, icon: "calendar-days",      order: 57, parentKey: "vendors.admin-root", requiredPermissions: ["vendors:credit-terms:write"]);

        // ── Operation UI — day-to-day vendor & AP management ──────────────────
        yield return Nav("op-root",           "Vendors",            "/vendors",                           UiContext.Operation, icon: "truck",              order: 55, isGroup: true);
        yield return Nav("list",              "All Vendors",        "/vendors",                           UiContext.Operation, icon: "list-bullet",        order: 56, parentKey: "vendors.op-root",    requiredPermissions: ["vendors:read"]);
        yield return Nav("ap-profile",        "AP Profiles",        "/vendors/ap-profiles",              UiContext.Operation, icon: "identification",      order: 57, parentKey: "vendors.op-root",    requiredPermissions: ["vendors:ap:read"]);
        yield return Nav("bank-accounts",     "Vendor Bank Accts",  "/vendors/bank-accounts",            UiContext.Operation, icon: "credit-card",        order: 58, parentKey: "vendors.op-root",    requiredPermissions: ["vendors:bank-accounts:read"]);
        yield return Nav("bills",             "Vendor Bills (AP)",  "/vendors/bills",                    UiContext.Operation, icon: "document-text",      order: 59, parentKey: "vendors.op-root",    requiredPermissions: ["vendors:bills:read"]);
        yield return Nav("overdue",           "Overdue Payables",   "/vendors/bills/overdue",            UiContext.Operation, icon: "exclamation-circle", order: 60, parentKey: "vendors.op-root",    requiredPermissions: ["vendors:bills:read"]);
    }

    public override IEnumerable<PermissionDescriptor> GetPermissions()
    {
        // Core vendor management
        yield return Perm("vendors",          "read",    "View Vendors",                    defaultRoles: ["admin", "accountant", "manager", "staff"]);
        yield return Perm("vendors",          "write",   "Create & Edit Vendors",           defaultRoles: ["admin", "accountant", "manager"]);
        yield return Perm("vendors",          "delete",  "Delete Vendors",                  defaultRoles: ["admin"],              isSensitive: true);

        // AP Profile
        yield return Perm("ap",               "read",    "View AP Profiles",                defaultRoles: ["admin", "accountant", "manager"]);
        yield return Perm("ap",               "write",   "Manage AP Profiles",              defaultRoles: ["admin", "accountant"],  isSensitive: true);

        // Vendor Bills
        yield return Perm("bills",            "read",    "View Vendor Bills",               defaultRoles: ["admin", "accountant", "manager"]);
        yield return Perm("bills",            "write",   "Create & Edit Vendor Bills",      defaultRoles: ["admin", "accountant"]);
        yield return Perm("bills",            "approve", "Approve Vendor Bills",            defaultRoles: ["admin"],              isSensitive: true);
        yield return Perm("bills",            "pay",     "Post Payments on Vendor Bills",   defaultRoles: ["admin", "accountant"],  isSensitive: true);

        // Bank Accounts
        yield return Perm("bank-accounts",    "read",    "View Vendor Bank Accounts",       defaultRoles: ["admin", "accountant", "manager"]);
        yield return Perm("bank-accounts",    "write",   "Manage Vendor Bank Accounts",     defaultRoles: ["admin", "accountant"],  isSensitive: true);

        // Vendor Classes & Credit Terms (admin config)
        yield return Perm("classes",          "write",   "Manage Vendor Classes",           defaultRoles: ["admin"],              isSensitive: true);
        yield return Perm("credit-terms",     "write",   "Manage Credit Terms",             defaultRoles: ["admin", "accountant"],  isSensitive: true);
    }

    public override IEnumerable<WidgetDescriptor> GetWidgets()
    {
        yield return new WidgetDescriptor(
            WidgetKey:    "vendors.overdue-payables",
            DisplayName:  "Overdue Payables",
            Description:  "Overdue vendor bills grouped by vendor with total outstanding amount.",
            ModuleId:     ModuleId,
            DataEndpoint: "/api/v1/businesses/{businessId}/vendor-bills/overdue/by-vendor",
            DefaultColSpan: 6, DefaultRowSpan: 2, MinColSpan: 4);

        yield return new WidgetDescriptor(
            WidgetKey:    "vendors.active-count",
            DisplayName:  "Active Vendors",
            Description:  "Count of active vendors registered to your business.",
            ModuleId:     ModuleId,
            DataEndpoint: "/api/v1/vendors?businessId={businessId}",
            DefaultColSpan: 3, DefaultRowSpan: 1, MinColSpan: 2);
    }
}
