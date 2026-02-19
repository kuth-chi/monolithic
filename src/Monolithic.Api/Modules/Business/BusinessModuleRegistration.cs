using Monolithic.Api.Modules.Business.Application;

namespace Monolithic.Api.Modules.Business;

public static class BusinessModuleRegistration
{
    /// <summary>
    /// Registers all Business module services:
    /// - Purchasing accounting (currency, COA, RFQ → PO → VendorBill, costing)
    /// - Multi-business ownership + licensing
    /// - Multi-branch management
    /// - Business settings, media (logo/cover), holidays, attendance policies
    /// </summary>
    public static IServiceCollection AddBusinessModule(this IServiceCollection services)
    {
        // ── Existing: Purchasing &amp; Accounting ──────────────────────────────────
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<IChartOfAccountService, ChartOfAccountService>();
        services.AddScoped<IVendorBillService, VendorBillService>();
        services.AddScoped<IEstimatePurchaseOrderService, EstimatePurchaseOrderService>();
        services.AddScoped<ICostingService, CostingService>();

        // ── Accounts Payable (vendor management &amp; payment) ─────────────────────
        services.AddScoped<IVendorCreditTermService, VendorCreditTermService>();
        services.AddScoped<IVendorClassService, VendorClassService>();
        services.AddScoped<IVendorProfileService, VendorProfileService>();
        services.AddScoped<IApDashboardService, ApDashboardService>();
        services.AddScoped<IApPaymentSessionService, ApPaymentSessionService>();
        services.AddScoped<IApCreditNoteService, ApCreditNoteService>();
        services.AddScoped<IApPaymentScheduleService, ApPaymentScheduleService>();

        // ── Multi-business / Multi-branch ───────────────────────────────────
        services.AddScoped<IBusinessLicenseService, BusinessLicenseService>();
        services.AddScoped<IBusinessOwnershipService, BusinessOwnershipService>();
        services.AddScoped<IBusinessBranchService, BusinessBranchService>();
        services.AddScoped<IBusinessSettingService, BusinessSettingService>();
        services.AddScoped<IBusinessMediaService, BusinessMediaService>();
        services.AddScoped<IBusinessHolidayService, BusinessHolidayService>();
        services.AddScoped<IAttendancePolicyService, AttendancePolicyService>();

        return services;
    }
}
