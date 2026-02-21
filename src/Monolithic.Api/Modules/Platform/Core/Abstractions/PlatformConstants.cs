namespace Monolithic.Api.Modules.Platform.Core.Abstractions;

/// <summary>
/// Centralised constants consumed across the Platform module.
/// Keeping them here prevents magic strings from spreading across the codebase (DRY).
/// </summary>
public static class PlatformConstants
{
    // ── Claim types ───────────────────────────────────────────────────────────
    public const string BusinessIdClaim = "business_id";
    public const string UserIdClaim     = "sub";
    public const string LocaleClaim     = "locale";
    public const string TimezoneClaim   = "timezone";

    // ── Cache key prefixes ────────────────────────────────────────────────────
    public const string FeatureFlagCachePrefix   = "ff:";
    public const string ThemeProfileCachePrefix  = "theme:";
    public const string UserPrefCachePrefix      = "upref:";
    public const string TemplateCachePrefix      = "tpl:";

    // ── Cache TTLs ───────────────────────────────────────────────────────────
    public static readonly TimeSpan FeatureFlagCacheTtl  = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan ThemeCacheTtl        = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan UserPrefCacheTtl     = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan TemplateCacheTtl     = TimeSpan.FromMinutes(60);

    // ── Notification channels ─────────────────────────────────────────────────
    public const string EmailChannel = "email";
    public const string SmsChannel   = "sms";
    public const string PushChannel  = "push";

    // ── System template slugs ─────────────────────────────────────────────────
    public const string WelcomeEmailSlug          = "system.welcome.email";
    public const string PasswordResetEmailSlug    = "system.password-reset.email";
    public const string TwoFactorSmsSlug          = "system.2fa.sms";
    public const string InvoicePdfSlug            = "invoice.pdf";
    public const string PurchaseOrderPdfSlug      = "purchase-order.pdf";

    // ── Widget keys ───────────────────────────────────────────────────────────
    public const string RevenueChartWidget        = "finance.revenue-chart";
    public const string LowStockWidget            = "inventory.low-stock";
    public const string PendingOrdersWidget       = "purchase-orders.pending";
    public const string CustomerCountWidget       = "customers.count";
    public const string RecentActivityWidget      = "platform.recent-activity";
}
