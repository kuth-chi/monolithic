using Monolithic.Api.Modules.PurchaseOrders.Application;

namespace Monolithic.Api.Modules.PurchaseOrders;

public static class PurchaseOrdersModuleRegistration
{
    public static IServiceCollection AddPurchaseOrdersModule(this IServiceCollection services)
    {
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        return services;
    }
}
