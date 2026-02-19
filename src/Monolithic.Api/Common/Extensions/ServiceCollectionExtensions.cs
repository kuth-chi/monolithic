using Monolithic.Api.Common.Caching;
using Monolithic.Api.Common.Configuration;
using Monolithic.Api.Common.Storage;
using Monolithic.Api.Modules.Analytics;
using Monolithic.Api.Modules.Business;
using Monolithic.Api.Modules.Customers;
using Monolithic.Api.Modules.Finance;
using Monolithic.Api.Modules.Identity;
using Monolithic.Api.Modules.Inventory;
using Monolithic.Api.Modules.PurchaseOrders;
using Monolithic.Api.Modules.Users;
using Monolithic.Api.Modules.Vendors;

namespace Monolithic.Api.Common.Extensions;

public static class ServiceCollectionExtensions
{
    private const string DefaultCorsPolicy = "DefaultCorsPolicy";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddOpenApi();
        services.AddHealthChecks();
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        // Local image storage (swap for AzureBlobImageStorageService / S3ImageStorageService in production)
        services.AddScoped<IImageStorageService, LocalImageStorageService>();

        // Distributed cache — InMemory in Development, Redis in Production
        if (environment.IsDevelopment())
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            var redisConnectionString = configuration[$"{InfrastructureOptions.SectionName}:{nameof(InfrastructureOptions.Redis)}:{nameof(RedisOptions.ConnectionString)}"];

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = string.IsNullOrWhiteSpace(redisConnectionString)
                    ? "localhost:6379"
                    : redisConnectionString;
            });
        }

        services.AddSingleton<ITwoLevelCache, TwoLevelCache>();

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultCorsPolicy, policyBuilder =>
            {
                policyBuilder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        services.Configure<InfrastructureOptions>(configuration.GetSection(InfrastructureOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        services.AddIdentityModule(configuration, environment);
        services.AddUsersModule();
        services.AddAnalyticsModule();
        services.AddBusinessModule();
        services.AddFinanceModule();
        services.AddInventoryModule();
        services.AddCustomersModule();
        services.AddVendorsModule();
        services.AddPurchaseOrdersModule();

        return services;
    }

    public static WebApplication UseApiPipeline(this WebApplication app, IWebHostEnvironment environment)
    {
        app.UseExceptionHandler();

        if (environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // Serve uploaded images from wwwroot/images/
        app.UseStaticFiles();

        app.UseCors(DefaultCorsPolicy);
        app.UseIdentityModule();

        app.MapControllers();
        app.MapHealthChecks("/healthz");

        return app;
    }
}