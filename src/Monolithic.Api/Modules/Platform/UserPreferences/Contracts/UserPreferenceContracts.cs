using Monolithic.Api.Modules.Platform.UserPreferences.Domain;

namespace Monolithic.Api.Modules.Platform.UserPreferences.Contracts;

public sealed record UserPreferenceDto(
    Guid Id,
    Guid UserId,
    Guid? BusinessId,
    string? PreferredLocale,
    string? PreferredTimezone,
    Guid? PreferredThemeId,
    string ColorScheme,
    DashboardLayout DashboardLayout,
    bool EmailNotificationsEnabled,
    bool SmsNotificationsEnabled,
    bool PushNotificationsEnabled,
    DateTimeOffset? ModifiedAtUtc
);

public sealed record UpdateUserPreferenceRequest(
    Guid UserId,
    Guid? BusinessId,
    string? PreferredLocale,
    string? PreferredTimezone,
    Guid? PreferredThemeId,
    string? ColorScheme,
    DashboardLayout? DashboardLayout,
    bool? EmailNotificationsEnabled,
    bool? SmsNotificationsEnabled,
    bool? PushNotificationsEnabled
);

public sealed record UpdateDashboardLayoutRequest(
    Guid UserId,
    Guid? BusinessId,
    DashboardLayout Layout
);
