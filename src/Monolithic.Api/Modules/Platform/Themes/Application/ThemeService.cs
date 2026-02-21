using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Themes.Contracts;
using Monolithic.Api.Modules.Platform.Themes.Domain;

namespace Monolithic.Api.Modules.Platform.Themes.Application;

public sealed class ThemeService(
    ApplicationDbContext db,
    IDistributedCache cache,
    ILogger<ThemeService> logger) : IThemeService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async Task<ThemeProfileDto?> GetDefaultAsync(Guid? businessId, CancellationToken ct = default)
    {
        var cacheKey = $"{PlatformConstants.ThemeProfileCachePrefix}{businessId ?? Guid.Empty}";
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null) return JsonSerializer.Deserialize<ThemeProfileDto>(cached, _json);

        // Business profile → system fallback
        var profile = businessId.HasValue
            ? await db.ThemeProfiles.AsNoTracking()
                .FirstOrDefaultAsync(t => t.BusinessId == businessId && t.IsDefault, ct)
              ?? await db.ThemeProfiles.AsNoTracking()
                .FirstOrDefaultAsync(t => t.BusinessId == null && t.IsDefault, ct)
            : await db.ThemeProfiles.AsNoTracking()
                .FirstOrDefaultAsync(t => t.BusinessId == null && t.IsDefault, ct);

        if (profile is null) return null;

        var dto = profile.ToDto();
        await cache.SetStringAsync(cacheKey,
            JsonSerializer.Serialize(dto, _json),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = PlatformConstants.ThemeCacheTtl },
            ct);
        return dto;
    }

    public async Task<IReadOnlyList<ThemeProfileDto>> ListAsync(
        Guid? businessId, CancellationToken ct = default)
    {
        return await db.ThemeProfiles.AsNoTracking()
            .Where(t => t.BusinessId == businessId)
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .Select(t => t.ToDto())
            .ToListAsync(ct);
    }

    public async Task<ThemeProfileDto> UpsertAsync(
        UpsertThemeProfileRequest req, CancellationToken ct = default)
    {
        var existing = await db.ThemeProfiles
            .FirstOrDefaultAsync(t => t.BusinessId == req.BusinessId && t.Name == req.Name, ct);

        if (existing is null)
        {
            existing = new ThemeProfile { Id = Guid.NewGuid(), BusinessId = req.BusinessId };
            db.ThemeProfiles.Add(existing);
        }

        existing.Name             = req.Name;
        existing.Description      = req.Description;
        existing.ColorPrimary     = req.ColorPrimary;
        existing.ColorSecondary   = req.ColorSecondary;
        existing.ColorAccent      = req.ColorAccent;
        existing.ColorSuccess     = req.ColorSuccess;
        existing.ColorWarning     = req.ColorWarning;
        existing.ColorDanger      = req.ColorDanger;
        existing.ColorInfo        = req.ColorInfo;
        existing.ColorBackground  = req.ColorBackground;
        existing.ColorSurface     = req.ColorSurface;
        existing.ColorBorder      = req.ColorBorder;
        existing.ColorText        = req.ColorText;
        existing.ColorTextMuted   = req.ColorTextMuted;
        existing.FontFamily       = req.FontFamily;
        existing.FontFamilyMono   = req.FontFamilyMono;
        existing.FontSizeBase     = req.FontSizeBase;
        existing.FontScaleRatio   = req.FontScaleRatio;
        existing.SpacingUnit      = req.SpacingUnit;
        existing.BorderRadiusSm   = req.BorderRadiusSm;
        existing.BorderRadiusMd   = req.BorderRadiusMd;
        existing.BorderRadiusLg   = req.BorderRadiusLg;
        existing.BorderRadiusFull = req.BorderRadiusFull;
        existing.ShadowSm         = req.ShadowSm;
        existing.ShadowMd         = req.ShadowMd;
        existing.ShadowLg         = req.ShadowLg;
        existing.SidebarWidth     = req.SidebarWidth;
        existing.TopbarHeight     = req.TopbarHeight;
        existing.ContentMaxWidth  = req.ContentMaxWidth;
        existing.SidebarPosition  = req.SidebarPosition;
        existing.ExtensionTokensJson = req.ExtensionTokensJson;
        existing.ModifiedAtUtc    = DateTimeOffset.UtcNow;

        if (req.SetAsDefault)
        {
            // Demote other defaults within the same business scope
            await db.ThemeProfiles
                .Where(t => t.BusinessId == req.BusinessId && t.Id != existing.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDefault, false), ct);
            existing.IsDefault = true;
        }

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(
            $"{PlatformConstants.ThemeProfileCachePrefix}{req.BusinessId ?? Guid.Empty}", ct);

        logger.LogInformation("[Theme] Upserted profile '{Name}' for business {BizId}",
            req.Name, req.BusinessId);

        return existing.ToDto();
    }

    public async Task SetDefaultAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await db.ThemeProfiles.FirstOrDefaultAsync(t => t.Id == profileId, ct)
            ?? throw new KeyNotFoundException($"ThemeProfile {profileId} not found.");

        await db.ThemeProfiles
            .Where(t => t.BusinessId == profile.BusinessId && t.Id != profileId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDefault, false), ct);

        profile.IsDefault = true;
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(
            $"{PlatformConstants.ThemeProfileCachePrefix}{profile.BusinessId ?? Guid.Empty}", ct);
    }

    public async Task DeleteAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await db.ThemeProfiles.FirstOrDefaultAsync(t => t.Id == profileId, ct)
            ?? throw new KeyNotFoundException($"ThemeProfile {profileId} not found.");

        db.ThemeProfiles.Remove(profile);
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(
            $"{PlatformConstants.ThemeProfileCachePrefix}{profile.BusinessId ?? Guid.Empty}", ct);
    }
}

file static class ThemeMappers
{
    public static ThemeProfileDto ToDto(this ThemeProfile t) => new(
        t.Id, t.BusinessId, t.Name, t.Description, t.IsDefault,
        t.ColorPrimary, t.ColorSecondary, t.ColorAccent,
        t.ColorSuccess, t.ColorWarning, t.ColorDanger, t.ColorInfo,
        t.ColorBackground, t.ColorSurface, t.ColorBorder, t.ColorText, t.ColorTextMuted,
        t.FontFamily, t.FontFamilyMono, t.FontSizeBase, t.FontScaleRatio,
        t.SpacingUnit,
        t.BorderRadiusSm, t.BorderRadiusMd, t.BorderRadiusLg, t.BorderRadiusFull,
        t.ShadowSm, t.ShadowMd, t.ShadowLg,
        t.SidebarWidth, t.TopbarHeight, t.ContentMaxWidth, t.SidebarPosition,
        t.ExtensionTokensJson, t.CreatedAtUtc);
}
