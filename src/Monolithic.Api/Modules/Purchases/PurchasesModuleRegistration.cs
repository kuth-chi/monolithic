using Microsoft.Extensions.DependencyInjection;
using Monolithic.Api.Modules.Purchases.Application;

namespace Monolithic.Api.Modules.Purchases;

/// <summary>
/// DI registration for the Purchases module.
/// Note: IVendorService is now registered by VendorsModuleRegistration
/// (Vendors module is a required dependency of Purchases).
/// </summary>
public static class PurchasesModuleRegistration
{
    public static IServiceCollection AddPurchasesModule(this IServiceCollection services)
    {
        // ── Purchase Orders ─────────────────────────────────────────────────────
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();

        return services;
    }
}
