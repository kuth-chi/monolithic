using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Monolithic.Api.Common.BackgroundServices;
using Monolithic.Api.Common.Caching;
using Monolithic.Api.Common.Configuration;
using Monolithic.Api.Common.Errors;
using Monolithic.Api.Common.Storage;
using Monolithic.Api.Modules.Analytics;
using Monolithic.Api.Modules.Business;
using Monolithic.Api.Modules.Finance;
using Monolithic.Api.Modules.Identity;
using Monolithic.Api.Modules.Inventory;
using Monolithic.Api.Modules.Purchases;
using Monolithic.Api.Modules.Sales;
using Monolithic.Api.Modules.Users;
using Monolithic.Api.Modules.Platform;

namespace Monolithic.Api.Common.Extensions;

public static class ServiceCollectionExtensions
{
    private const string DefaultCorsPolicy = "DefaultCorsPolicy";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddOpenApi();
        services.AddMemoryCache();

        // ── Infrastructure health checks ─────────────────────────────────────
        var infraSection = configuration.GetSection(InfrastructureOptions.SectionName);
        var pgConnStr    = infraSection[$"{nameof(InfrastructureOptions.PostgreSql)}:{nameof(PostgresOptions.ConnectionString)}"] ?? string.Empty;
        var redisConnStr = infraSection[$"{nameof(InfrastructureOptions.Redis)}:{nameof(RedisOptions.ConnectionString)}"] ?? string.Empty;

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is running."),
                tags: ["live", "ready"])
            .AddNpgSql(pgConnStr,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "db"])
            .AddRedis(redisConnStr,
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready", "cache"]);
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
        services.AddSalesModule();
        services.AddPurchasesModule();

        // ── Platform Foundation (plug-and-play core) ──────────────────────────
        services.AddPlatformModule(configuration);

        // ── Soft-delete purge runner ───────────────────────────────────────────
        // Reads SoftDeletePurge:SystemDefaultRetentionDays and SoftDeletePurge:RunIntervalHours
        // from appsettings.json (falls back to built-in defaults: 30 days / 24 h).
        var purgeOptions = configuration
            .GetSection(SoftDeletePurgeOptions.SectionName)
            .Get<SoftDeletePurgeOptions>() ?? new SoftDeletePurgeOptions();
        services.AddSingleton(purgeOptions);
        services.AddHostedService<SoftDeletePurgeService>();

        return services;
    }

    public static WebApplication UseApiPipeline(this WebApplication app, IWebHostEnvironment environment)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.Title = "Monolithic API";
                options.Theme = ScalarTheme.DeepSpace;
            });
        }

        // Serve uploaded images from wwwroot/images/
        app.UseStaticFiles();

        app.UseCors(DefaultCorsPolicy);
        app.UseIdentityModule();

        app.MapControllers();

        // /healthz — Kubernetes-style liveness/readiness probe with JSON body
        app.MapHealthChecks("/healthz", new()
        {
            ResponseWriter = async (ctx, report) =>
            {
                ctx.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    status    = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    duration  = report.TotalDuration.TotalMilliseconds,
                    checks    = report.Entries.Select(e => new
                    {
                        name        = e.Key,
                        status      = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration    = e.Value.Duration.TotalMilliseconds,
                        tags        = e.Value.Tags,
                        error       = e.Value.Exception?.Message,
                    }),
                });
                await ctx.Response.WriteAsync(result);
            },
        });

        return app;
    }
}