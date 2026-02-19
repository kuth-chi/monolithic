using Monolithic.Api.Modules.Inventory.Application;

namespace Monolithic.Api.Modules.Inventory;

public static class InventoryModuleRegistration
{
    /// <summary>
    /// Registers Inventory, Warehouse, and media (variant/image) module services.
    /// </summary>
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IInventoryImageService, InventoryImageService>();

        return services;
    }
}
