using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Monolithic.Api.Common.BackgroundServices;
using Monolithic.Api.Common.Caching;
using Monolithic.Api.Common.Configuration;
using Monolithic.Api.Common.Errors;
using Monolithic.Api.Common.Security;
using Monolithic.Api.Common.Storage;
using Monolithic.Api.Common.Validation;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Platform.Core.Infrastructure;

namespace Monolithic.Api.Common.Extensions;

public static class ServiceCollectionExtensions
{
    private const string DefaultCorsPolicy = "DefaultCorsPolicy";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // ── Controllers + validation filter ──────────────────────────────────
        services.AddControllers(options =>
        {
            // OWASP A03: Automatically validates every [FromBody] request using
            // any registered FluentValidation IValidator<T> before the action runs.
            options.Filters.Add<ValidationActionFilter>();
        });

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddOpenApi();
        services.AddMemoryCache();

        // ── FluentValidation — auto-register all validators in this assembly ──
        // OWASP A03 — input validation at the boundary before any logic runs.
        services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);

        // ── OWASP A03: Input sanitization ─────────────────────────────────────
        services.AddSingleton<IInputSanitizer, InputSanitizer>();

        // ── Security headers config ───────────────────────────────────────────
        // Reads EnableStrictCsp / EnableHsts from appsettings; falls back to
        // safe defaults (strict CSP off in Dev, HSTS off unless TLS is confirmed).
        services.Configure<SecurityHeadersOptions>(
            configuration.GetSection(SecurityHeadersOptions.SectionName));

        // ── Rate limiting ─────────────────────────────────────────────────────
        // OWASP A04 + A07: prevents brute-force and DoS via per-endpoint limits.
        services.AddApiRateLimiting(configuration);

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

        // ── CORS — OWASP A05: restrictive policy; wildcard only in Development ─
        // In Production, set Cors:AllowedOrigins in appsettings.Production.json.
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultCorsPolicy, policyBuilder =>
            {
                if (environment.IsDevelopment() || allowedOrigins.Length == 0)
                {
                    // Development only — wildcard is acceptable for local dev
                    policyBuilder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS");
                }
                else
                {
                    // Production — explicit origin allowlist (OWASP A05)
                    policyBuilder
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                        .AllowCredentials();
                }
            });
        });

        services.Configure<InfrastructureOptions>(configuration.GetSection(InfrastructureOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        // ── Soft-delete purge runner ───────────────────────────────────────────
        var purgeOptions = configuration
            .GetSection(SoftDeletePurgeOptions.SectionName)
            .Get<SoftDeletePurgeOptions>() ?? new SoftDeletePurgeOptions();
        services.AddSingleton(purgeOptions);
        services.AddHostedService<SoftDeletePurgeService>();

        // ── License Guard options + Background Monitor + Tamper Detective ──────
        var licenseGuardOptions = configuration
            .GetSection(LicenseGuardOptions.SectionName)
            .Get<LicenseGuardOptions>() ?? new LicenseGuardOptions();
        services.AddSingleton(licenseGuardOptions);
        services.AddHostedService<LicenseExpirationMonitorService>();
        services.AddHostedService<LicenseTamperMonitorService>();  // Fake License Detective (2h)

        // ── Module Discovery — plug-and-play OS kernel ────────────────────────
        // Creates the registry eagerly (before DI build) and calls Discover()
        // so every IModule.RegisterServices() is invoked here in dependency order.
        // The pre-built singleton keeps the populated module list for injection
        // into controllers (PlatformInfoController, etc.) after the host builds.
        //
        // To add a NEW module: implement IModule (or extend ModuleBase) anywhere
        // in this assembly — zero wiring needed here.
        var registry = new ModuleRegistry();
        registry.Discover(services, configuration, environment, typeof(Program).Assembly);
        services.AddSingleton(registry);   // pre-built instance wins over any TryAdd

        return services;
    }

    public static WebApplication UseApiPipeline(this WebApplication app, IWebHostEnvironment environment)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        // ── OWASP security headers — must be first in pipeline ────────────────
        app.UseSecurityHeaders();

        // ── Rate limiting ──────────────────────────────────────────────────────
        app.UseRateLimiter();

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

        // ── Module pipeline configuration (includes Identity UseAuthentication/Authorization) ─
        // ConfigureAll() calls each IModule.ConfigurePipeline() in dependency order.
        // IdentityModule is first (root) and adds UseAuthentication + UseAuthorization.
        var registry = app.Services.GetRequiredService<ModuleRegistry>();
        registry.SetLogger(app.Services.GetRequiredService<ILogger<ModuleRegistry>>());
        registry.ConfigureAll(app);

        // ── License Validation Gate ─────────────────────────────────────────
        // Must be placed AFTER authentication/authorization but BEFORE controllers.
        app.UseMiddleware<LicenseValidationMiddleware>();

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