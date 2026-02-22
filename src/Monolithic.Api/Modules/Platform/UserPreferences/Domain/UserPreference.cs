using System.Text.Json;

namespace Monolithic.Api.Modules.Platform.UserPreferences.Domain;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Validates IANA timezone identifiers using the host OS timezone database.
/// On Linux/macOS this resolves IANA IDs directly; on Windows it uses the
/// .NET 6+ cross-platform timezone mapping.
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public static class TimezoneValidator
{
    private const int MinPageSize = 5;
    private const int MaxPageSize = 100;

    /// <summary>
    /// Returns <c>true</c> when <paramref name="timezoneId"/> is a valid
    /// IANA or Windows timezone recognised by the runtime.
    /// </summary>
    public static bool IsValid(string? timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId)) return false;
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
    }

    /// <summary>Validates <paramref name="pageSize"/> is within allowed bounds [5–100].</summary>
    public static bool IsValidPageSize(int pageSize)
        => pageSize is >= MinPageSize and <= MaxPageSize;

    /// <summary>Returns all IANA/system timezone IDs available on the host.</summary>
    public static IReadOnlyList<TimezoneInfo> GetAll()
        => TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => new TimezoneInfo(tz.Id, tz.DisplayName, tz.BaseUtcOffset))
            .OrderBy(t => t.UtcOffset)
            .ThenBy(t => t.Id)
            .ToList();
}

/// <summary>Summary info for a single timezone entry returned from the discovery endpoint.</summary>
public sealed record TimezoneInfo(string Id, string DisplayName, TimeSpan UtcOffset);



// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Per-user preferences scoped to a business.
///
/// Stored as typed columns for commonly queried fields (locale, timezone, theme)
/// and a JSONB column (<see cref="DashboardLayoutJson"/>) for the flexible
/// widget/layout configuration so the schema never needs to change when new
/// widgets are added.
///
/// One record per (UserId, BusinessId) pair.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public class UserPreference
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// Scopes the preference to a specific business.
    /// Null = global preference applied across all businesses for this user.
    /// </summary>
    public Guid? BusinessId { get; set; }

    // ── Localisation preferences ──────────────────────────────────────────────

    /// <summary>User's preferred IETF locale, e.g. "en-US", "km-KH".</summary>
    public string? PreferredLocale { get; set; }

    /// <summary>User's IANA timezone override, e.g. "Asia/Phnom_Penh".</summary>
    public string? PreferredTimezone { get; set; }

    // ── Theme preference ──────────────────────────────────────────────────────

    /// <summary>User-selected ThemeProfile ID (overrides business default).</summary>
    public Guid? PreferredThemeId { get; set; }

    /// <summary>"light", "dark", "system" — CSS prefers-color-scheme preference.</summary>
    public string ColorScheme { get; set; } = "system";

    // ── Layout & widgets ──────────────────────────────────────────────────────

    /// <summary>
    /// JSONB blob representing <see cref="DashboardLayout"/>.
    /// Flexible: any widget can add its own placement config here.
    /// </summary>
    public string? DashboardLayoutJson { get; set; }

    // ── Pagination preference ─────────────────────────────────────────────────

    /// <summary>
    /// User's preferred page size for all paginated lists.
    /// Must be between 5 and 100. Defaults to 20.
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;

    // ── Notification preferences ──────────────────────────────────────────────

    /// <summary>Opt-in to email notifications.</summary>
    public bool EmailNotificationsEnabled { get; set; } = true;

    /// <summary>Opt-in to SMS notifications.</summary>
    public bool SmsNotificationsEnabled { get; set; } = false;

    /// <summary>Opt-in to browser push notifications.</summary>
    public bool PushNotificationsEnabled { get; set; } = true;

    // ── Audit ─────────────────────────────────────────────────────────────────

    public DateTimeOffset CreatedAtUtc   { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>Typed representation of the dashboard layout JSON blob.</summary>
// ─────────────────────────────────────────────────────────────────────────────
public sealed class DashboardLayout
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    /// <summary>Ordered list of widget placements configured by this user.</summary>
    public List<WidgetPlacement> Widgets { get; set; } = [];

    /// <summary>
    /// Number of CSS grid columns (typically 12).
    /// Allows users to switch between compact and wide layouts.
    /// </summary>
    public int GridColumns { get; set; } = 12;

    public static DashboardLayout Empty => new();

    public static DashboardLayout? Deserialize(string? json)
        => string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<DashboardLayout>(json, _json);

    public string Serialize()
        => JsonSerializer.Serialize(this, _json);
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Position and size of one widget in the dashboard grid.
/// Follows CSS Grid conventions (1-based column/row indexes).
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public sealed class WidgetPlacement
{
    /// <summary>Stable widget key, e.g. "finance.revenue-chart".</summary>
    public string WidgetKey { get; set; } = string.Empty;

    public int ColStart  { get; set; } = 1;
    public int ColSpan   { get; set; } = 4;
    public int RowStart  { get; set; } = 1;
    public int RowSpan   { get; set; } = 2;
    public bool Visible  { get; set; } = true;

    /// <summary>Arbitrary per-widget settings (e.g. chart period, refresh interval).</summary>
    public Dictionary<string, object?> Settings { get; set; } = [];
}
