using Monolithic.Api.Modules.Platform.Core;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Business;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Business Foundation module — multi-business ownership, licensing, branches,
/// settings, vendors, chart-of-accounts, and purchasing backbone.
///
/// This is the root module that nearly all other modules depend on because it
/// establishes the BusinessId / BranchId tenant context boundaries.
///
/// UI Shell : Admin    (setup, licensing, branch management)
///            Operation (vendor management, AP, costing, settings)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class BusinessModule : ModuleBase
{
    public override string  ModuleId    => "business";
    public override string  DisplayName => "Business Management";
    public override string  Version     => "1.0.0";
    public override string  Description => "Multi-business ownership, branch management, licensing, vendors, and business settings.";
    public override string  Icon        => "building-office-2";

    public override void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        => services.AddBusinessModule();

    public override IEnumerable<NavigationItem> GetNavigationItems()
    {
        // ── Admin UI ──────────────────────────────────────────────────────────
        yield return Nav("admin-root",    "Business",       "/admin/business",                   UiContext.Admin, icon: "building-office-2",  order: 20, isGroup: true);
        yield return Nav("licenses",      "Licenses",       "/admin/business/licenses",          UiContext.Admin, icon: "identification",      order: 21, parentKey: "business.admin-root", requiredPermissions: ["business:licenses:read"]);
        yield return Nav("owners",        "Ownership",      "/admin/business/owners",            UiContext.Admin, icon: "user-group",          order: 22, parentKey: "business.admin-root", requiredPermissions: ["business:owners:admin"]);
        yield return Nav("branches",      "Branches",       "/admin/business/branches",          UiContext.Admin, icon: "map-pin",             order: 23, parentKey: "business.admin-root", requiredPermissions: ["business:branches:read"]);
        yield return Nav("holidays",      "Holidays",       "/admin/business/holidays",          UiContext.Admin, icon: "calendar-days",       order: 24, parentKey: "business.admin-root", requiredPermissions: ["business:settings:write"]);
        yield return Nav("attendance",    "Attendance Policy","/admin/business/attendance-policy",UiContext.Admin, icon: "clock",              order: 25, parentKey: "business.admin-root", requiredPermissions: ["business:settings:write"]);

        // ── Operation UI ──────────────────────────────────────────────────────
        yield return Nav("op-root",       "Business",       "/business",                         UiContext.Operation, icon: "building-office-2", order: 10, isGroup: true);
        yield return Nav("settings",      "Settings",       "/business/settings",                UiContext.Operation, icon: "cog-6-tooth",       order: 11, parentKey: "business.op-root",    requiredPermissions: ["business:settings:read"]);
        yield return Nav("media",         "Branding",       "/business/branding",                UiContext.Operation, icon: "photo",             order: 12, parentKey: "business.op-root",    requiredPermissions: ["business:settings:write"]);
        yield return Nav("coa",           "Chart of Accounts","/business/chart-of-accounts",     UiContext.Operation, icon: "list-bullet",       order: 13, parentKey: "business.op-root",    requiredPermissions: ["business:coa:read"]);
        yield return Nav("currencies",    "Currencies",     "/business/currencies",              UiContext.Operation, icon: "currency-dollar",   order: 14, parentKey: "business.op-root",    requiredPermissions: ["business:currencies:read"]);
        yield return Nav("ap",            "Accounts Payable","/business/accounts-payable",       UiContext.Operation, icon: "arrow-down-circle",  order: 15, parentKey: "business.op-root",   requiredPermissions: ["business:ap:read"]);
    }

    public override IEnumerable<PermissionDescriptor> GetPermissions()
    {
        // Business settings
        yield return Perm("settings",  "read",   "View Business Settings",      defaultRoles: ["admin", "manager"]);
        yield return Perm("settings",  "write",  "Edit Business Settings",      defaultRoles: ["admin"],              isSensitive: true);
        // Licenses
        yield return Perm("licenses",  "read",   "View Licenses",               defaultRoles: ["admin"]);
        yield return Perm("licenses",  "admin",  "Manage Licenses",             defaultRoles: ["admin"],              isSensitive: true);
        // Ownership
        yield return Perm("owners",    "admin",  "Manage Business Ownership",   defaultRoles: ["admin"],              isSensitive: true);
        // Branches
        yield return Perm("branches",  "read",   "View Branches",               defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("branches",  "write",  "Manage Branches",             defaultRoles: ["admin"],              isSensitive: true);
        // Chart of Accounts
        yield return Perm("coa",       "read",   "View Chart of Accounts",      defaultRoles: ["admin", "accountant"]);
        yield return Perm("coa",       "write",  "Manage Chart of Accounts",    defaultRoles: ["admin", "accountant"], isSensitive: true);
        // Currencies
        yield return Perm("currencies","read",   "View Currencies",             defaultRoles: ["admin", "accountant", "staff"]);
        yield return Perm("currencies","write",  "Manage Currencies",           defaultRoles: ["admin", "accountant"], isSensitive: true);
        // Accounts Payable
        yield return Perm("ap",        "read",   "View Accounts Payable",       defaultRoles: ["admin", "accountant", "manager"]);
        yield return Perm("ap",        "write",  "Process Accounts Payable",    defaultRoles: ["admin", "accountant"], isSensitive: true);
        yield return Perm("ap",        "approve","Approve AP Payments",         defaultRoles: ["admin"],               isSensitive: true);
        // Vendors
        yield return Perm("vendors",   "read",   "View Vendors",                defaultRoles: ["admin", "accountant", "manager", "staff"]);
        yield return Perm("vendors",   "write",  "Manage Vendors",              defaultRoles: ["admin", "accountant"]);
        // Costing
        yield return Perm("costing",   "read",   "View Costing",                defaultRoles: ["admin", "accountant", "manager"]);
        yield return Perm("costing",   "write",  "Manage Costing",              defaultRoles: ["admin", "accountant"]);
    }
}
