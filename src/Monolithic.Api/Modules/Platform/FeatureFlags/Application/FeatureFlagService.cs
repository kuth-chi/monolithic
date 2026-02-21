using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.FeatureFlags.Contracts;
using Monolithic.Api.Modules.Platform.FeatureFlags.Domain;

namespace Monolithic.Api.Modules.Platform.FeatureFlags.Application;

// ─────────────────────────────────────────────────────────────────────────────
public interface IFeatureFlagService
{
    /// <summary>
    /// Check if a feature is enabled using scope fall-through:
    /// User → Business → System.
    /// Returns false if no flag record exists (safe default).
    /// </summary>
    Task<bool> IsEnabledAsync(string key, Guid? businessId = null, Guid? userId = null, CancellationToken ct = default);

    /// <summary>Returns the resolved result with the scope that matched.</summary>
    Task<FeatureFlagCheckResult> CheckAsync(string key, Guid? businessId = null, Guid? userId = null, CancellationToken ct = default);

    Task<IReadOnlyList<FeatureFlagDto>> ListAsync(FeatureFlagScope? scope = null, Guid? businessId = null, CancellationToken ct = default);

    Task<FeatureFlagDto> UpsertAsync(UpsertFeatureFlagRequest req, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════════════
public sealed class FeatureFlagService(
    ApplicationDbContext db,
    IDistributedCache cache,
    ILogger<FeatureFlagService> logger) : IFeatureFlagService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async Task<bool> IsEnabledAsync(
        string key, Guid? businessId = null, Guid? userId = null, CancellationToken ct = default)
    {
        var result = await CheckAsync(key, businessId, userId, ct);
        return result.IsEnabled;
    }

    public async Task<FeatureFlagCheckResult> CheckAsync(
        string key, Guid? businessId = null, Guid? userId = null, CancellationToken ct = default)
    {
        var cacheKey = $"{PlatformConstants.FeatureFlagCachePrefix}{key}:{businessId}:{userId}";
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<FeatureFlagCheckResult>(cached, _json)!;

        var now = DateTimeOffset.UtcNow;

        FeatureFlag? flag = null;

        // User override
        if (userId.HasValue)
            flag = await db.FeatureFlags.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Key == key
                    && f.Scope == FeatureFlagScope.User
                    && f.UserId == userId
                    && (f.ExpiresAtUtc == null || f.ExpiresAtUtc > now), ct);

        // Business override
        if (flag is null && businessId.HasValue)
            flag = await db.FeatureFlags.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Key == key
                    && f.Scope == FeatureFlagScope.Business
                    && f.BusinessId == businessId
                    && (f.ExpiresAtUtc == null || f.ExpiresAtUtc > now), ct);

        // System default
        if (flag is null)
            flag = await db.FeatureFlags.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Key == key
                    && f.Scope == FeatureFlagScope.System
                    && (f.ExpiresAtUtc == null || f.ExpiresAtUtc > now), ct);

        var result = flag is not null
            ? new FeatureFlagCheckResult(key, flag.IsEnabled, flag.Scope)
            : new FeatureFlagCheckResult(key, false, FeatureFlagScope.System);

        await cache.SetStringAsync(cacheKey,
            JsonSerializer.Serialize(result, _json),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = PlatformConstants.FeatureFlagCacheTtl },
            ct);

        return result;
    }

    public async Task<IReadOnlyList<FeatureFlagDto>> ListAsync(
        FeatureFlagScope? scope = null, Guid? businessId = null, CancellationToken ct = default)
    {
        var query = db.FeatureFlags.AsNoTracking();
        if (scope.HasValue)       query = query.Where(f => f.Scope == scope.Value);
        if (businessId.HasValue)  query = query.Where(f => f.BusinessId == businessId);

        return await query
            .OrderBy(f => f.Key)
            .Select(f => f.ToDto())
            .ToListAsync(ct);
    }

    public async Task<FeatureFlagDto> UpsertAsync(
        UpsertFeatureFlagRequest req, CancellationToken ct = default)
    {
        var flag = await db.FeatureFlags.FirstOrDefaultAsync(f =>
            f.Key == req.Key
            && f.Scope == req.Scope
            && f.BusinessId == req.BusinessId
            && f.UserId == req.UserId, ct);

        if (flag is null)
        {
            flag = new FeatureFlag { Id = Guid.NewGuid(), Key = req.Key, Scope = req.Scope };
            db.FeatureFlags.Add(flag);
        }

        flag.DisplayName  = req.DisplayName;
        flag.Description  = req.Description;
        flag.BusinessId   = req.BusinessId;
        flag.UserId       = req.UserId;
        flag.IsEnabled    = req.IsEnabled;
        flag.ExpiresAtUtc = req.ExpiresAtUtc;
        flag.MetadataJson = req.MetadataJson;
        flag.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        // Evict all cache entries for this key
        await cache.RemoveAsync($"{PlatformConstants.FeatureFlagCachePrefix}{req.Key}::", ct);
        await cache.RemoveAsync($"{PlatformConstants.FeatureFlagCachePrefix}{req.Key}:{req.BusinessId}:", ct);
        await cache.RemoveAsync($"{PlatformConstants.FeatureFlagCachePrefix}{req.Key}:{req.BusinessId}:{req.UserId}", ct);

        logger.LogInformation("[FeatureFlag] Upserted: {Key} → {Enabled}", req.Key, req.IsEnabled);
        return flag.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new KeyNotFoundException($"FeatureFlag {id} not found.");
        db.FeatureFlags.Remove(flag);
        await db.SaveChangesAsync(ct);
    }
}

// ── Mappers ───────────────────────────────────────────────────────────────────

file static class FfMappers
{
    public static FeatureFlagDto ToDto(this FeatureFlag f) => new(
        f.Id, f.Key, f.DisplayName, f.Description, f.Scope,
        f.BusinessId, f.UserId, f.IsEnabled, f.ExpiresAtUtc,
        f.MetadataJson, f.CreatedAtUtc);
}
