using System.ComponentModel.DataAnnotations;
using Monolithic.Api.Modules.Platform.UserPreferences.Domain;

namespace Monolithic.Api.Modules.Platform.UserPreferences.Contracts;

// ── Response ──────────────────────────────────────────────────────────────────

public sealed record UserPreferenceDto(
    Guid Id,
    Guid UserId,
    Guid? BusinessId,
    string? PreferredLocale,
    string? PreferredTimezone,
    Guid? PreferredThemeId,
    string ColorScheme,
    int DefaultPageSize,
    DashboardLayout DashboardLayout,
    bool EmailNotificationsEnabled,
    bool SmsNotificationsEnabled,
    bool PushNotificationsEnabled,
    DateTimeOffset? ModifiedAtUtc
);

// ── Update (full / partial) ───────────────────────────────────────────────────

public sealed record UpdateUserPreferenceRequest(
    Guid UserId,
    Guid? BusinessId,
    string? PreferredLocale,

    /// <summary>
    /// IANA timezone ID, e.g. "Asia/Phnom_Penh".
    /// Validated server-side; invalid IDs are rejected with HTTP 422.
    /// </summary>
    string? PreferredTimezone,

    Guid? PreferredThemeId,
    string? ColorScheme,

    /// <summary>
    /// Preferred page size for all paginated lists (5–100).
    /// </summary>
    [Range(5, 100)]
    int? DefaultPageSize,

    DashboardLayout? DashboardLayout,
    bool? EmailNotificationsEnabled,
    bool? SmsNotificationsEnabled,
    bool? PushNotificationsEnabled
);

/// <summary>
/// Payload used by the self-service "me" endpoints so the caller
/// does not need to supply their own UserId (it is resolved from the JWT).
/// </summary>
public sealed record UpdateMyPreferenceRequest(
    Guid? BusinessId,
    string? PreferredLocale,

    /// <summary>IANA timezone ID, e.g. "Asia/Phnom_Penh".</summary>
    string? PreferredTimezone,

    Guid? PreferredThemeId,
    string? ColorScheme,

    /// <summary>Preferred page size for all paginated lists (5–100).</summary>
    [Range(5, 100)]
    int? DefaultPageSize,

    DashboardLayout? DashboardLayout,
    bool? EmailNotificationsEnabled,
    bool? SmsNotificationsEnabled,
    bool? PushNotificationsEnabled
);

// ── Layout ────────────────────────────────────────────────────────────────────

public sealed record UpdateDashboardLayoutRequest(
    Guid UserId,
    Guid? BusinessId,
    DashboardLayout Layout
);

/// <summary>Self-service layout update (UserId resolved from JWT).</summary>
public sealed record UpdateMyDashboardLayoutRequest(
    Guid? BusinessId,
    DashboardLayout Layout
);

// Note: WidgetDescriptor is defined in Platform.Core.Abstractions (ModuleDescriptors.cs).
// Import Monolithic.Api.Modules.Platform.Core.Abstractions to use it.
