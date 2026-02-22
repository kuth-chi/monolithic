using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monolithic.Api.Common.Configuration;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Core.Infrastructure;
using Monolithic.Api.Modules.Platform.FeatureFlags.Application;
using Monolithic.Api.Modules.Platform.Infrastructure.Data;
using Monolithic.Api.Modules.Platform.Notifications.Application;
using Monolithic.Api.Modules.Platform.Notifications.Application.Channels;
using Monolithic.Api.Modules.Platform.Templates.Application;
using Monolithic.Api.Modules.Platform.Themes.Application;
using Monolithic.Api.Modules.Platform.UserPreferences.Application;

namespace Monolithic.Api.Modules.Platform;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Registers all Platform Foundation services:
///
///  ┌─────────────────────────────────────────────────────────┐
///  │  PLATFORM FOUNDATION                                    │
///  │  ─────────────────────────────────────────────────────  │
///  │  • ModuleRegistry  – plug-and-play auto-discovery        │
///  │  • ITenantContext  – multi-tenant request context        │
///  │  • ITemplateService + ITemplateRenderService (Scriban)   │
///  │  • IThemeService   – full design-token theme engine       │
///  │  • IUserPreferenceService – dashboard/widget layout      │
///  │  • IFeatureFlagService – per-business/user toggles       │
///  │  • INotificationService – Email/SMS/Push/InApp channels  │
///  └─────────────────────────────────────────────────────────┘
///
/// Called from ServiceCollectionExtensions.AddApiServices().
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public static class PlatformModuleRegistration
{
    public static IServiceCollection AddPlatformModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // ── Platform Foundation Database ──────────────────────────────────────
        services.AddDbContext<PlatformDbContext>(options =>
        {
            if (environment.IsDevelopment())
            {
                // Share the same SQLite file in Development for simplicity.
                // Each table lives in its own logical context but in one physical file.
                var sqliteConn = configuration
                    [$"{InfrastructureOptions.SectionName}:Databases:Platform:ConnectionString"]
                    ?? configuration
                    [$"{InfrastructureOptions.SectionName}:{nameof(InfrastructureOptions.SQLite)}:{nameof(SqliteOptions.ConnectionString)}"]
                    ?? "Data Source=monolithic_dev.db";
                options.UseSqlite(sqliteConn);
            }
            else
            {
                var pgConn = configuration
                    [$"{InfrastructureOptions.SectionName}:Databases:Platform:ConnectionString"];
                options.UseNpgsql(pgConn);
            }
        });

        services.AddScoped<IPlatformDbContext>(
            sp => sp.GetRequiredService<PlatformDbContext>());

        // ── Core infrastructure ───────────────────────────────────────────────
        // ModuleRegistry is pre-built and registered by ServiceCollectionExtensions
        // before Discover() is called; TryAdd ensures no duplicate registration.
        services.TryAddSingleton<ModuleRegistry>();
        services.AddScoped<ITenantContext, TenantContext>();

        // ── Templates (Scriban engine) ────────────────────────────────────────
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<ITemplateRenderService, ScribanTemplateRenderService>();

        // ── Themes ────────────────────────────────────────────────────────────
        services.AddScoped<IThemeService, ThemeService>();
        services.AddScoped<ILogoColorExtractor, LogoColorExtractor>();
        services.AddHttpClient("logo-extractor", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.Add("User-Agent", "MonolithicApi/1.0 LogoColorExtractor");
        });

        // ── User Preferences ──────────────────────────────────────────────────
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();

        // ── Feature Flags ─────────────────────────────────────────────────────
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();

        // ── Notification channels (Strategy pattern — all registered, routed
        //    by NotificationService based on SendNotificationRequest.Channel)
        services.AddScoped<INotificationChannel, SmtpEmailChannel>();
        services.AddScoped<INotificationChannel, StubSmsChannel>();
        services.AddScoped<INotificationChannel, StubPushChannel>();
        services.AddScoped<INotificationChannel, InAppNotificationChannel>();

        // ── Notification service (orchestrator) ───────────────────────────────
        services.AddScoped<INotificationService, NotificationService>();

        // ── Notification config ───────────────────────────────────────────────
        services.Configure<NotificationOptions>(
            configuration.GetSection(NotificationOptions.SectionName));

        return services;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>Strongly-typed configuration for notification providers.</summary>
// ─────────────────────────────────────────────────────────────────────────────
public sealed class NotificationOptions
{
    public const string SectionName = "Platform:Notifications";

    public SmtpOptions Smtp { get; set; } = new();

    public SmsProviderOptions Sms { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host        { get; set; } = "localhost";
    public int    Port        { get; set; } = 25;
    public bool   UseSsl      { get; set; } = false;
    public string FromAddress { get; set; } = "no-reply@monolithic.local";
    public string FromName    { get; set; } = "Monolithic Platform";
    public string? Username   { get; set; }
    public string? Password   { get; set; }
}

public sealed class SmsProviderOptions
{
    /// <summary>Provider name: "stub", "twilio", "aws-sns".</summary>
    public string Provider  { get; set; } = "stub";
    public string? ApiKey   { get; set; }
    public string? ApiSecret { get; set; }
    public string? FromNumber { get; set; }
}
