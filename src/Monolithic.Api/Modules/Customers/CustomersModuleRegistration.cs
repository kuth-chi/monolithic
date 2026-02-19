using Monolithic.Api.Modules.Customers.Application;

namespace Monolithic.Api.Modules.Customers;

public static class CustomersModuleRegistration
{
    public static IServiceCollection AddCustomersModule(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerContactService, CustomerContactService>();
        services.AddScoped<ICustomerAddressService, CustomerAddressService>();
        return services;
    }
}
