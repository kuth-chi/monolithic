using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monolithic.Api.Modules.Finance.Application;

namespace Monolithic.Api.Modules.Finance;

public static class FinanceModuleRegistration
{
    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        services.AddScoped<IBankAccountService, BankAccountService>();

        // General Ledger — Journal Entries
        services.AddScoped<IJournalEntryService, JournalEntryService>();

        // TimeProvider abstraction (enables deterministic unit-testing of timestamps)
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
