using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Core.Infrastructure;
using Monolithic.Api.Modules.Platform.UserPreferences.Contracts;
using Monolithic.Api.Modules.Platform.UserPreferences.Domain;

namespace Monolithic.Api.Modules.Platform.UserPreferences.Application;

public interface IUserPreferenceService
{
    /// <summary>Returns (or lazily creates) preferences for the given user / business pair.</summary>
    Task<UserPreferenceDto> GetOrCreateAsync(Guid userId, Guid? businessId, CancellationToken ct = default);

    /// <summary>Admin-facing: update any user's preferences.</summary>
    Task<UserPreferenceDto> UpdateAsync(UpdateUserPreferenceRequest req, CancellationToken ct = default);

    /// <summary>Admin-facing: update any user's dashboard layout.</summary>
    Task<UserPreferenceDto> UpdateLayoutAsync(UpdateDashboardLayoutRequest req, CancellationToken ct = default);

    /// <summary>Admin-facing: reset any user's dashboard layout to system defaults.</summary>
    Task ResetLayoutAsync(Guid userId, Guid? businessId, CancellationToken ct = default);

    // ── Self-service ("me") ───────────────────────────────────────────────────

    /// <summary>Self-service: update the calling user's own preferences.</summary>
    Task<UserPreferenceDto> UpdateMyAsync(Guid currentUserId, UpdateMyPreferenceRequest req, CancellationToken ct = default);

    /// <summary>Self-service: update the calling user's own dashboard layout.</summary>
    Task<UserPreferenceDto> UpdateMyLayoutAsync(Guid currentUserId, UpdateMyDashboardLayoutRequest req, CancellationToken ct = default);

    /// <summary>Self-service: reset the calling user's own layout to system defaults.</summary>
    Task ResetMyLayoutAsync(Guid currentUserId, Guid? businessId, CancellationToken ct = default);

    // ── Discovery ──────────────────────────────────────────────────────────────

    /// <summary>Returns all IANA/system timezone entries available on the host, ordered by UTC offset.</summary>
    IReadOnlyList<TimezoneInfo> GetAvailableTimezones();

    /// <summary>Returns the catalogue of dashboard widgets contributed by all modules.</summary>
    IEnumerable<WidgetDescriptor> GetAvailableWidgets();
}

// ═══════════════════════════════════════════════════════════════════════════════
public sealed class UserPreferenceService(
    IPlatformDbContext db,
    IDistributedCache cache,
    ModuleRegistry moduleRegistry,
    ILogger<UserPreferenceService> logger) : IUserPreferenceService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<UserPreferenceDto> GetOrCreateAsync(
        Guid userId, Guid? businessId, CancellationToken ct = default)
    {
        var cacheKey = CacheKey(userId, businessId);
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null) return JsonSerializer.Deserialize<UserPreferenceDto>(cached, _json)!;

        var pref = await db.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.BusinessId == businessId, ct);

        if (pref is null)
        {
            pref = new UserPreference
            {
                Id         = Guid.NewGuid(),
                UserId     = userId,
                BusinessId = businessId,
            };
            db.UserPreferences.Add(pref);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("[UserPref] Created default preferences for user {UserId}", userId);
        }

        var dto = pref.ToDto();
        await CacheAsync(cacheKey, dto, ct);
        return dto;
    }

    // ── Admin: full update ────────────────────────────────────────────────────

    public async Task<UserPreferenceDto> UpdateAsync(
        UpdateUserPreferenceRequest req, CancellationToken ct = default)
    {
        ValidateTimezone(req.PreferredTimezone);
        ValidatePageSize(req.DefaultPageSize);

        var pref = await EnsureExistsAsync(req.UserId, req.BusinessId, ct);

        ApplyCommonFields(pref,
            req.PreferredLocale, req.PreferredTimezone, req.PreferredThemeId,
            req.ColorScheme, req.DefaultPageSize,
            req.EmailNotificationsEnabled, req.SmsNotificationsEnabled, req.PushNotificationsEnabled,
            req.DashboardLayout);

        pref.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var dto = pref.ToDto();
        await CacheAsync(CacheKey(req.UserId, req.BusinessId), dto, ct);
        return dto;
    }

    // ── Admin: layout only ────────────────────────────────────────────────────

    public async Task<UserPreferenceDto> UpdateLayoutAsync(
        UpdateDashboardLayoutRequest req, CancellationToken ct = default)
    {
        var pref = await EnsureExistsAsync(req.UserId, req.BusinessId, ct);
        pref.DashboardLayoutJson = req.Layout.Serialize();
        pref.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var dto = pref.ToDto();
        await CacheAsync(CacheKey(req.UserId, req.BusinessId), dto, ct);
        return dto;
    }

    // ── Admin: reset layout ───────────────────────────────────────────────────

    public async Task ResetLayoutAsync(Guid userId, Guid? businessId, CancellationToken ct = default)
    {
        var pref = await EnsureExistsAsync(userId, businessId, ct);
        pref.DashboardLayoutJson = null;
        pref.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKey(userId, businessId), ct);
    }

    // ── Self-service: update own preferences ─────────────────────────────────

    public async Task<UserPreferenceDto> UpdateMyAsync(
        Guid currentUserId, UpdateMyPreferenceRequest req, CancellationToken ct = default)
    {
        ValidateTimezone(req.PreferredTimezone);
        ValidatePageSize(req.DefaultPageSize);

        var pref = await EnsureExistsAsync(currentUserId, req.BusinessId, ct);

        ApplyCommonFields(pref,
            req.PreferredLocale, req.PreferredTimezone, req.PreferredThemeId,
            req.ColorScheme, req.DefaultPageSize,
            req.EmailNotificationsEnabled, req.SmsNotificationsEnabled, req.PushNotificationsEnabled,
            req.DashboardLayout);

        pref.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var dto = pref.ToDto();
        await CacheAsync(CacheKey(currentUserId, req.BusinessId), dto, ct);
        return dto;
    }

    // ── Self-service: update own layout ──────────────────────────────────────

    public async Task<UserPreferenceDto> UpdateMyLayoutAsync(
        Guid currentUserId, UpdateMyDashboardLayoutRequest req, CancellationToken ct = default)
    {
        var pref = await EnsureExistsAsync(currentUserId, req.BusinessId, ct);
        pref.DashboardLayoutJson = req.Layout.Serialize();
        pref.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var dto = pref.ToDto();
        await CacheAsync(CacheKey(currentUserId, req.BusinessId), dto, ct);
        return dto;
    }

    // ── Self-service: reset own layout ───────────────────────────────────────

    public async Task ResetMyLayoutAsync(Guid currentUserId, Guid? businessId, CancellationToken ct = default)
    {
        var pref = await EnsureExistsAsync(currentUserId, businessId, ct);
        pref.DashboardLayoutJson = null;
        pref.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKey(currentUserId, businessId), ct);
    }

    // ── Discovery ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public IReadOnlyList<TimezoneInfo> GetAvailableTimezones()
        => TimezoneValidator.GetAll();

    /// <inheritdoc/>
    /// <remarks>
    /// Delegates to <see cref="ModuleRegistry.GetAllWidgets"/> so every
    /// module's widget contributions are included automatically.
    /// </remarks>
    public IEnumerable<WidgetDescriptor> GetAvailableWidgets()
        => moduleRegistry.GetAllWidgets();

    // ── Shared mutation helper ────────────────────────────────────────────────

    private static void ApplyCommonFields(
        UserPreference pref,
        string? locale, string? timezone, Guid? themeId, string? colorScheme, int? pageSize,
        bool? emailNotif, bool? smsNotif, bool? pushNotif,
        DashboardLayout? layout)
    {
        if (locale       is not null) pref.PreferredLocale   = locale;
        if (timezone     is not null) pref.PreferredTimezone = timezone;
        if (themeId      is not null) pref.PreferredThemeId  = themeId;
        if (colorScheme  is not null) pref.ColorScheme       = colorScheme;
        if (pageSize     is not null) pref.DefaultPageSize   = pageSize.Value;
        if (emailNotif   is not null) pref.EmailNotificationsEnabled = emailNotif.Value;
        if (smsNotif     is not null) pref.SmsNotificationsEnabled   = smsNotif.Value;
        if (pushNotif    is not null) pref.PushNotificationsEnabled  = pushNotif.Value;
        if (layout       is not null) pref.DashboardLayoutJson       = layout.Serialize();
    }

    // ── Validation helpers ────────────────────────────────────────────────────

    /// <summary>Throws <see cref="ArgumentException"/> when timezone is non-null and invalid.</summary>
    private static void ValidateTimezone(string? timezoneId)
    {
        if (timezoneId is not null && !TimezoneValidator.IsValid(timezoneId))
            throw new ArgumentException(
                $"'{timezoneId}' is not a recognised timezone ID. " +
                "Use GET /api/v1/preferences/timezones for a list of valid IDs.",
                nameof(timezoneId));
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> when pageSize is non-null and out of range.</summary>
    private static void ValidatePageSize(int? pageSize)
    {
        if (pageSize is not null && !TimezoneValidator.IsValidPageSize(pageSize.Value))
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                $"Page size must be between 5 and 100. Supplied: {pageSize.Value}.");
    }

    // ── DB helpers ────────────────────────────────────────────────────────────

    private async Task<UserPreference> EnsureExistsAsync(
        Guid userId, Guid? businessId, CancellationToken ct)
    {
        var pref = await db.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.BusinessId == businessId, ct);

        if (pref is not null) return pref;

        pref = new UserPreference { Id = Guid.NewGuid(), UserId = userId, BusinessId = businessId };
        db.UserPreferences.Add(pref);
        return pref;
    }

    private async Task CacheAsync(string key, UserPreferenceDto dto, CancellationToken ct)
        => await cache.SetStringAsync(key,
            JsonSerializer.Serialize(dto, _json),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = PlatformConstants.UserPrefCacheTtl },
            ct);

    private static string CacheKey(Guid userId, Guid? businessId)
        => $"{PlatformConstants.UserPrefCachePrefix}{userId}:{businessId ?? Guid.Empty}";
}

// ── Mappers ───────────────────────────────────────────────────────────────────

file static class UserPrefMappers
{
    public static UserPreferenceDto ToDto(this UserPreference p) => new(
        p.Id, p.UserId, p.BusinessId,
        p.PreferredLocale, p.PreferredTimezone, p.PreferredThemeId, p.ColorScheme,
        p.DefaultPageSize,
        DashboardLayout.Deserialize(p.DashboardLayoutJson) ?? DashboardLayout.Empty,
        p.EmailNotificationsEnabled, p.SmsNotificationsEnabled, p.PushNotificationsEnabled,
        p.ModifiedAtUtc);
}

