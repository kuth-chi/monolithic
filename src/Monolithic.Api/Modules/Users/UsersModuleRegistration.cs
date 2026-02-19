using Monolithic.Api.Modules.Users.Application;

namespace Monolithic.Api.Modules.Users;

public static class UsersModuleRegistration
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddScoped<IUserService, IdentityBackedUserService>();
        return services;
    }
}