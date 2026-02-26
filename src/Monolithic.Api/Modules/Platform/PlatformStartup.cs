using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Core.Infrastructure;
using Monolithic.Api.Modules.Platform.Infrastructure;
using Monolithic.Api.Modules.Platform.Templates.Application;
using Monolithic.Api.Modules.Platform.Themes.Application;
using Monolithic.Api.Modules.Platform.Themes.Contracts;

namespace Monolithic.Api.Modules.Platform;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// One-time startup tasks for the Platform Foundation module.
///
/// Idempotent — safe to run on every restart; operations check for existing
/// data before inserting to avoid duplicates.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public static class PlatformStartup
{
    public static async Task InitializePlatformAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp     = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<PlatformFoundationMarker>>();

        logger.LogInformation("[Platform] Running startup initialization...");

        // ── Step 1: Migrate all module databases before seeding data ──────────
        var config = sp.GetRequiredService<IConfiguration>();
        await ModuleDatabaseInitializer.MigrateAllAsync(scope, config, logger);

        // ── Step 2: Module first-run hooks (seed reference data) ──────────────
        await RunModuleFirstRunHooksAsync(scope);
        try
        {
            await SeedDefaultTemplatesAsync(scope);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[Platform] Default template seeding failed. Startup will continue, but templates may be incomplete.");
        }

        try
        {
            await SeedSystemThemeAsync(scope);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[Platform] System theme seeding failed. Startup will continue, but default theme may be missing.");
        }

        logger.LogInformation("[Platform] Startup initialization complete.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Calls <see cref="IModule.OnFirstRunAsync"/> for every registered module
    /// so each module can seed its own reference data on fresh installations.
    /// Runs in topological order (same order as registration).
    /// </summary>
    private static async Task RunModuleFirstRunHooksAsync(IServiceScope scope)
    {
        var registry = scope.ServiceProvider.GetRequiredService<ModuleRegistry>();
        var logger   = scope.ServiceProvider.GetRequiredService<ILogger<PlatformFoundationMarker>>();

        foreach (var module in registry.Modules)
        {
            try
            {
                logger.LogDebug("[Platform] Running OnFirstRunAsync for module: {Id}", module.ModuleId);
                await module.OnFirstRunAsync(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[Platform] OnFirstRunAsync failed for module '{Id}'. " +
                    "This is non-fatal — the system will continue, but module data may be incomplete.",
                    module.ModuleId);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedDefaultTemplatesAsync(IServiceScope scope)
    {
        var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
        var registry        = scope.ServiceProvider.GetRequiredService<ModuleRegistry>();

        var descriptors = registry.GetAllDefaultTemplates().ToList();

        // Also seed built-in platform templates
        descriptors.AddRange(GetBuiltInTemplates());

        await templateService.SeedDefaultsAsync(descriptors);
    }

    private static async Task SeedSystemThemeAsync(IServiceScope scope)
    {
        var themeService = scope.ServiceProvider.GetRequiredService<IThemeService>();

        // Only seed if no system theme exists
        var existing = await themeService.GetDefaultAsync(businessId: null);
        if (existing is not null) return;

        await themeService.UpsertAsync(new UpsertThemeProfileRequest(
            BusinessId:      null,
            Name:            "Default",
            Description:     "System default theme. Businesses can override at their scope.",
            ColorPrimary:    "#2563EB",
            ColorSecondary:  "#7C3AED",
            ColorAccent:     "#F59E0B",
            ColorSuccess:    "#10B981",
            ColorWarning:    "#F59E0B",
            ColorDanger:     "#EF4444",
            ColorInfo:       "#3B82F6",
            ColorBackground: "#FFFFFF",
            ColorSurface:    "#F9FAFB",
            ColorBorder:     "#E5E7EB",
            ColorText:       "#111827",
            ColorTextMuted:  "#6B7280",
            FontFamily:      "Inter, sans-serif",
            FontFamilyMono:  "'JetBrains Mono', monospace",
            FontSizeBase:    "16px",
            FontScaleRatio:  1.25m,
            SpacingUnit:     4,
            BorderRadiusSm:  "4px",
            BorderRadiusMd:  "8px",
            BorderRadiusLg:  "12px",
            BorderRadiusFull:"9999px",
            ShadowSm:        "0 1px 2px 0 rgb(0 0 0 / 0.05)",
            ShadowMd:        "0 4px 6px -1px rgb(0 0 0 / 0.1)",
            ShadowLg:        "0 10px 15px -3px rgb(0 0 0 / 0.1)",
            SidebarWidth:    "256px",
            TopbarHeight:    "64px",
            ContentMaxWidth: "1280px",
            SidebarPosition: "left",
            ExtensionTokensJson: null,
            SetAsDefault:    true));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // System-level default templates (welcome, auth, etc.)
    // ─────────────────────────────────────────────────────────────────────────
    private static IEnumerable<DefaultTemplateDescriptor> GetBuiltInTemplates()
    {
        yield return new DefaultTemplateDescriptor(
            Slug:               PlatformConstants.WelcomeEmailSlug,
            DisplayName:        "Welcome Email",
            TemplateType:       "Email",
            DefaultSubject:     "Welcome to {{ business_name }}!",
            DefaultContent:     """
                <h1>Welcome, {{ user_name }}!</h1>
                <p>Thank you for joining <strong>{{ business_name }}</strong>.</p>
                <p>Your account is ready. <a href="{{ login_url }}">Sign in now</a>.</p>
                """,
            AvailableVariables: "user_name,business_name,login_url");

        yield return new DefaultTemplateDescriptor(
            Slug:               PlatformConstants.PasswordResetEmailSlug,
            DisplayName:        "Password Reset",
            TemplateType:       "Email",
            DefaultSubject:     "Reset your password – {{ business_name }}",
            DefaultContent:     """
                <p>Hi {{ user_name }},</p>
                <p>We received a request to reset your password.</p>
                <p><a href="{{ reset_url }}">Reset my password</a> (expires in {{ expires_minutes }} minutes)</p>
                <p>If you did not request this, you can safely ignore this email.</p>
                """,
            AvailableVariables: "user_name,business_name,reset_url,expires_minutes");

        yield return new DefaultTemplateDescriptor(
            Slug:               PlatformConstants.TwoFactorSmsSlug,
            DisplayName:        "Two-Factor SMS",
            TemplateType:       "Sms",
            DefaultContent:     "{{ business_name }}: Your verification code is {{ code }}. Expires in {{ expires_minutes }} min.",
            AvailableVariables: "business_name,code,expires_minutes");

        yield return new DefaultTemplateDescriptor(
            Slug:               PlatformConstants.InvoicePdfSlug,
            DisplayName:        "Invoice PDF",
            TemplateType:       "Pdf",
            DefaultContent:     """
                {
                    "title": "Invoice #{{ invoice_number }}",
                    "date": "{{ invoice_date }}",
                    "due_date": "{{ due_date }}",
                    "business": {
                        "name": "{{ business_name }}",
                        "address": "{{ business_address }}",
                        "logo_url": "{{ business_logo_url }}"
                    },
                    "customer": {
                        "name": "{{ customer_name }}",
                        "address": "{{ customer_address }}"
                    },
                    "items": "{{ items_json }}",
                    "subtotal": "{{ subtotal }}",
                    "tax": "{{ tax }}",
                    "total": "{{ total }}",
                    "currency": "{{ currency_code }}",
                    "notes": "{{ notes }}"
                }
                """,
            AvailableVariables: "invoice_number,invoice_date,due_date,business_name,business_address,business_logo_url,customer_name,customer_address,items_json,subtotal,tax,total,currency_code,notes");

        yield return new DefaultTemplateDescriptor(
            Slug:               PlatformConstants.PurchaseOrderPdfSlug,
            DisplayName:        "Purchase Order PDF",
            TemplateType:       "Pdf",
            DefaultContent:     """
                {
                    "title": "Purchase Order #{{ po_number }}",
                    "date": "{{ po_date }}",
                    "business": { "name": "{{ business_name }}", "logo_url": "{{ logo_url }}" },
                    "vendor": { "name": "{{ vendor_name }}", "address": "{{ vendor_address }}" },
                    "items": "{{ items_json }}",
                    "total": "{{ total }}",
                    "currency": "{{ currency_code }}"
                }
                """,
            AvailableVariables: "po_number,po_date,business_name,logo_url,vendor_name,vendor_address,items_json,total,currency_code");
    }
}

/// <summary>Marker type for logger category — avoids leaking implementation class names.</summary>
internal sealed class PlatformFoundationMarker;
