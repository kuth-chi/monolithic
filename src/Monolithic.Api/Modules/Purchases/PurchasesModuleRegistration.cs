using Microsoft.Extensions.DependencyInjection;
using Monolithic.Api.Modules.Purchases.Application;
using Monolithic.Api.Modules.Purchases.Vendors.Application;

namespace Monolithic.Api.Modules.Purchases;

public static class PurchasesModuleRegistration
{
    public static IServiceCollection AddPurchasesModule(this IServiceCollection services)
    {
        // ── Vendors ──────────────────────────────────────────────────────────────
        services.AddScoped<IVendorService, VendorService>();

        // ── Purchase Orders ─────────────────────────────────────────────────────
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();

        return services;
    }
}
