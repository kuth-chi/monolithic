using Monolithic.Api.Modules.Finance.Application;

namespace Monolithic.Api.Modules.Finance;

public static class FinanceModuleRegistration
{
    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        services.AddScoped<IBankAccountService, BankAccountService>();
        return services;
    }
}
