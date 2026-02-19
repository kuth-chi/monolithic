using Monolithic.Api.Modules.Vendors.Application;

namespace Monolithic.Api.Modules.Vendors;

public static class VendorsModuleRegistration
{
    public static IServiceCollection AddVendorsModule(this IServiceCollection services)
    {
        services.AddScoped<IVendorService, VendorService>();
        return services;
    }
}
