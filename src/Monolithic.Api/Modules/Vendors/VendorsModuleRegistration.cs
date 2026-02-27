using Microsoft.Extensions.DependencyInjection;
using Monolithic.Api.Modules.Vendors.Application;

namespace Monolithic.Api.Modules.Vendors;

/// <summary>
/// DI registration for the Vendors module.
/// Called by <see cref="VendorsModule.RegisterServices"/> during startup.
/// </summary>
public static class VendorsModuleRegistration
{
    public static IServiceCollection AddVendorsModule(this IServiceCollection services)
    {
        // ── Core Vendor Service ───────────────────────────────────────────────
        services.AddScoped<IVendorService, VendorService>();

        return services;
    }
}
