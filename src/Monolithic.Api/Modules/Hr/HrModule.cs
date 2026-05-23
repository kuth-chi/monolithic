using Monolithic.Api.Modules.Platform.Core;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Hr;

public sealed class HrModule : ModuleBase
{
    public override string ModuleId => "hr";
    public override string DisplayName => "Human Resources";
    public override string Version => "1.0.0";
    public override string Description => "Employee directory, attendance, leave, payroll, and people operations workflows.";
    public override string Icon => "users";
    public override IEnumerable<string> Dependencies => ["business", "users"];

    public override void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        => services.AddHrModule();

    public override IEnumerable<NavigationItem> GetNavigationItems()
    {
        yield return Nav("root", "HR", "/hr", UiContext.Operation, icon: "users", order: 70, isGroup: true);
        yield return Nav("employees", "Employees", "/hr/employees", UiContext.Operation, icon: "user-group", order: 71, parentKey: "hr.root", requiredPermissions: ["hr:employees:read"]);
        yield return Nav("attendance", "Attendance", "/hr/attendance", UiContext.Operation, icon: "clock", order: 72, parentKey: "hr.root", requiredPermissions: ["hr:attendance:read"]);
        yield return Nav("leave", "Leave Management", "/hr/leave", UiContext.Operation, icon: "calendar-days", order: 73, parentKey: "hr.root", requiredPermissions: ["hr:leave:read"]);
        yield return Nav("payroll", "Payroll", "/hr/payroll", UiContext.Operation, icon: "banknotes", order: 74, parentKey: "hr.root", requiredPermissions: ["hr:payroll:read"]);
        yield return Nav("org", "Org Structure", "/hr/org-structure", UiContext.Operation, icon: "squares-2x2", order: 75, parentKey: "hr.root", requiredPermissions: ["hr:org:read"]);
    }

    public override IEnumerable<PermissionDescriptor> GetPermissions()
    {
        yield return Perm("employees", "read", "View Employees", defaultRoles: ["admin", "manager", "staff", "Owner", "System Admin", "Manager", "Staff"]);
        yield return Perm("employees", "write", "Manage Employees", defaultRoles: ["admin", "manager", "Owner", "System Admin", "Manager"], isSensitive: true);
        yield return Perm("employees", "delete", "Deactivate Employees", defaultRoles: ["admin", "Owner", "System Admin"], isSensitive: true);

        yield return Perm("attendance", "read", "View Attendance", defaultRoles: ["admin", "manager", "Owner", "System Admin", "Manager"]);
        yield return Perm("attendance", "write", "Manage Attendance", defaultRoles: ["admin", "manager", "Owner", "System Admin", "Manager"], isSensitive: true);

        yield return Perm("leave", "read", "View Leave Requests", defaultRoles: ["admin", "manager", "Owner", "System Admin", "Manager"]);
        yield return Perm("leave", "write", "Manage Leave Requests", defaultRoles: ["admin", "manager", "Owner", "System Admin", "Manager"], isSensitive: true);
        yield return Perm("leave", "approve", "Approve Leave Requests", defaultRoles: ["admin", "manager", "Owner", "System Admin", "Manager"], isSensitive: true);

        yield return Perm("payroll", "read", "View Payroll", defaultRoles: ["admin", "Owner", "System Admin"]);
        yield return Perm("payroll", "write", "Manage Payroll", defaultRoles: ["admin", "Owner", "System Admin"], isSensitive: true);
        yield return Perm("payroll", "approve", "Approve Payroll", defaultRoles: ["admin", "Owner", "System Admin"], isSensitive: true);

        yield return Perm("org", "read", "View Organization Structure", defaultRoles: ["admin", "manager", "Owner", "System Admin", "Manager"]);
        yield return Perm("org", "write", "Manage Organization Structure", defaultRoles: ["admin", "Owner", "System Admin"], isSensitive: true);
    }
}
