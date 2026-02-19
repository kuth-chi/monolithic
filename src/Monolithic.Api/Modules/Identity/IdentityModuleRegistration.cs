using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Common.Configuration;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Identity.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Identity;

public static class IdentityModuleRegistration
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // EF Core DbContext — SQLite in Development, PostgreSQL in Production
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (environment.IsDevelopment())
            {
                var sqliteConnectionString = configuration[$"{InfrastructureOptions.SectionName}:{nameof(InfrastructureOptions.SQLite)}:{nameof(SqliteOptions.ConnectionString)}"]
                    ?? "Data Source=monolithic_dev.db";
                options.UseSqlite(sqliteConnectionString);
            }
            else
            {
                var pgConnectionString = configuration[$"{InfrastructureOptions.SectionName}:{nameof(InfrastructureOptions.PostgreSql)}:{nameof(PostgresOptions.ConnectionString)}"];
                options.UseNpgsql(pgConnectionString);
            }
        });

        // ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

            options.User.RequireUniqueEmail = true;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Authorization handlers
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

    public static WebApplication UseIdentityModule(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static async Task InitializeIdentityAsync(this WebApplication app)
    {
        await SeedData.SeedAsync(app.Services);
    }
}
