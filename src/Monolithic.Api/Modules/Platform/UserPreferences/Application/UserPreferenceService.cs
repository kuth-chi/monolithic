using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.UserPreferences.Contracts;
using Monolithic.Api.Modules.Platform.UserPreferences.Domain;

namespace Monolithic.Api.Modules.Platform.UserPreferences.Application;

public interface IUserPreferenceService
{
    Task<UserPreferenceDto> GetOrCreateAsync(Guid userId, Guid? businessId, CancellationToken ct = default);
    Task<UserPreferenceDto> UpdateAsync(UpdateUserPreferenceRequest req, CancellationToken ct = default);
    Task<UserPreferenceDto> UpdateLayoutAsync(UpdateDashboardLayoutRequest req, CancellationToken ct = default);
    Task ResetLayoutAsync(Guid userId, Guid? businessId, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════════════
public sealed class UserPreferenceService(
    ApplicationDbContext db,
    IDistributedCache cache,
    ILogger<UserPreferenceService> logger) : IUserPreferenceService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

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

    public async Task<UserPreferenceDto> UpdateAsync(
        UpdateUserPreferenceRequest req, CancellationToken ct = default)
    {
        var pref = await EnsureExistsAsync(req.UserId, req.BusinessId, ct);

        if (req.PreferredLocale       is not null) pref.PreferredLocale       = req.PreferredLocale;
        if (req.PreferredTimezone     is not null) pref.PreferredTimezone     = req.PreferredTimezone;
        if (req.PreferredThemeId      is not null) pref.PreferredThemeId      = req.PreferredThemeId;
        if (req.ColorScheme           is not null) pref.ColorScheme           = req.ColorScheme;
        if (req.EmailNotificationsEnabled is not null) pref.EmailNotificationsEnabled = req.EmailNotificationsEnabled.Value;
        if (req.SmsNotificationsEnabled   is not null) pref.SmsNotificationsEnabled   = req.SmsNotificationsEnabled.Value;
        if (req.PushNotificationsEnabled  is not null) pref.PushNotificationsEnabled  = req.PushNotificationsEnabled.Value;
        if (req.DashboardLayout is not null) pref.DashboardLayoutJson = req.DashboardLayout.Serialize();

        pref.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var dto = pref.ToDto();
        await CacheAsync(CacheKey(req.UserId, req.BusinessId), dto, ct);
        return dto;
    }

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

    public async Task ResetLayoutAsync(Guid userId, Guid? businessId, CancellationToken ct = default)
    {
        var pref = await EnsureExistsAsync(userId, businessId, ct);
        pref.DashboardLayoutJson = null;
        pref.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKey(userId, businessId), ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
        DashboardLayout.Deserialize(p.DashboardLayoutJson) ?? DashboardLayout.Empty,
        p.EmailNotificationsEnabled, p.SmsNotificationsEnabled, p.PushNotificationsEnabled,
        p.ModifiedAtUtc);
}
