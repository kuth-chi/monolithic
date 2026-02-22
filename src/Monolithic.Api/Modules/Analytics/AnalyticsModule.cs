using Monolithic.Api.Modules.Platform.Core;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Analytics;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Analytics module — dashboard KPIs, trend charts, and business intelligence.
///
/// UI Shell : Operation (drill-down insights for operators)
///            Admin    (system-level analytics for administrators)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class AnalyticsModule : ModuleBase
{
    public override string  ModuleId    => "analytics";
    public override string  DisplayName => "Analytics & Reports";
    public override string  Version     => "1.0.0";
    public override string  Description => "Dashboard KPIs, trend charts, and business intelligence reports.";
    public override string  Icon        => "chart-bar";

    public override void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        => services.AddAnalyticsModule();

    public override IEnumerable<NavigationItem> GetNavigationItems()
    {
        // ── Operation UI ──────────────────────────────────────────────────────
        yield return Nav("root",       "Analytics",         "/analytics",             UiContext.Operation, icon: "chart-bar",          order: 80, isGroup: true);
        yield return Nav("dashboard",  "Dashboard",         "/analytics/dashboard",   UiContext.Operation, icon: "squares-2x2",        order: 81, parentKey: "analytics.root",      requiredPermissions: [Perms.Read]);
        yield return Nav("reports",    "Reports",           "/analytics/reports",     UiContext.Operation, icon: "document-chart-bar", order: 82, parentKey: "analytics.root",      requiredPermissions: [Perms.Read]);
        yield return Nav("trends",     "Trends",            "/analytics/trends",      UiContext.Operation, icon: "arrow-trending-up",  order: 83, parentKey: "analytics.root",      requiredPermissions: [Perms.Read]);

        // ── Admin UI ──────────────────────────────────────────────────────────
        yield return Nav("admin-root", "Analytics",         "/admin/analytics",       UiContext.Admin, icon: "chart-bar", order: 80, isGroup: true);
        yield return Nav("system",     "System Metrics",    "/admin/analytics/system",UiContext.Admin, icon: "cpu-chip",  order: 81, parentKey: "analytics.admin-root", requiredPermissions: [Perms.Admin]);
    }

    public override IEnumerable<PermissionDescriptor> GetPermissions()
    {
        yield return Perm("dashboard", "read",   "View Analytics Dashboard",  defaultRoles: ["admin", "manager", "staff"]);
        yield return Perm("reports",   "read",   "View Reports",              defaultRoles: ["admin", "manager"]);
        yield return Perm("reports",   "export", "Export Reports",            defaultRoles: ["admin", "manager"],            isSensitive: true);
        yield return Perm("system",    "admin",  "System Analytics (Admin)",  defaultRoles: ["admin"],                       isSensitive: true);
    }

    public override IEnumerable<WidgetDescriptor> GetWidgets()
    {
        yield return new WidgetDescriptor(
            WidgetKey:     PlatformConstants.RevenueChartWidget,
            DisplayName:   "Revenue Chart",
            Description:   "Displays monthly revenue trend for the current business.",
            ModuleId:      ModuleId,
            DataEndpoint:  "/api/v1/analytics/revenue-chart",
            DefaultColSpan: 8, DefaultRowSpan: 3, MinColSpan: 4);

        yield return new WidgetDescriptor(
            WidgetKey:     PlatformConstants.CustomerCountWidget,
            DisplayName:   "Customer Count",
            Description:   "Total active customers.",
            ModuleId:      ModuleId,
            DataEndpoint:  "/api/v1/analytics/customer-count",
            DefaultColSpan: 4, DefaultRowSpan: 2, MinColSpan: 2);
    }

    private static class Perms
    {
        public const string Read  = "analytics:dashboard:read";
        public const string Admin = "analytics:system:admin";
    }
}
