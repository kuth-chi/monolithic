using Monolithic.Api.Modules.Analytics.Application;

namespace Monolithic.Api.Modules.Analytics;

public static class AnalyticsModuleRegistration
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        services.AddScoped<IDashboardQueryService, InMemoryDashboardQueryService>();
        return services;
    }
}