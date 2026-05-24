namespace Monolithic.Api.Modules.Hr;

using Monolithic.Api.Modules.Hr.Application;

public static class HrModuleRegistration
{
    public static IServiceCollection AddHrModule(this IServiceCollection services)
    {
        services.AddScoped<IHrLeaveService, InMemoryHrLeaveService>();
        services.AddSingleton<IHrAttendanceService, InMemoryHrAttendanceService>();
        services.AddSingleton<IHrPayrollService, InMemoryHrPayrollService>();
        return services;
    }
}
