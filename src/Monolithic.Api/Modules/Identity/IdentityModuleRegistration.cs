using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Monolithic.Api.Common.Configuration;
using Monolithic.Api.Common.Data;
using Monolithic.Api.Modules.Identity.Application;
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
        // Also registered as IApplicationDbContext so all modules can depend on the
        // abstraction rather than the concrete infrastructure type.
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

            // BusinessBankAccount, CustomerBankAccount and VendorBankAccount all derive
            // from BankAccountBase (TPH hierarchy). EF Core prohibits HasQueryFilter on
            // derived TPH types, so their parent-filter cascade warning is unavoidable.
            // The entities are always accessed via filtered parent navigation properties,
            // so no orphaned rows will appear in practice.
            //
            // PendingModelChangesWarning is suppressed because migrations are generated
            // on SQLite (dev) but deployed on PostgreSQL (prod). EF Core detects a type
            // mismatch in the snapshot (INTEGER/TEXT vs PostgreSQL native types) and
            // incorrectly flags it as pending changes. The schema is correct in production.
            options.ConfigureWarnings(w =>
            {
                w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning);
                w.Ignore(RelationalEventId.PendingModelChangesWarning);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

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
        services.AddScoped<ValidateBusinessAccessFilter>();

        // Auth services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthAuditLogger, AuthAuditLogger>();

        services.AddHttpClient("seed-license-mapping", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Monolithic-SeedData/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        // JWT configuration
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var secretKey = jwtSection[nameof(JwtOptions.SecretKey)] ?? string.Empty;
        var issuer = jwtSection[nameof(JwtOptions.Issuer)] ?? "monolithic-api";
        var audience = jwtSection[nameof(JwtOptions.Audience)] ?? "monolithic-web";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // Preserve JWT claim names as-is (e.g. "sub") without mapping to
            // CLR URI-style claim types. Required so TenantContext.ParseGuid("sub")
            // can locate the subject claim without string-URI indirection.
            options.MapInboundClaims = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

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
