using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Monolithic.Api.Common.Color;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Themes.Contracts;
using Monolithic.Api.Modules.Platform.Themes.Domain;
using static Monolithic.Api.Modules.Platform.Themes.Application.ColorContrastHelper;

namespace Monolithic.Api.Modules.Platform.Themes.Application;

public sealed class ThemeService(
    IPlatformDbContext db,
    IDistributedCache cache,
    ILogoColorExtractor logoExtractor,
    ILogger<ThemeService> logger) : IThemeService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    // -- Read ------------------------------------------------------------------

    public async Task<ThemeProfileDto?> GetDefaultAsync(Guid? businessId, CancellationToken ct = default)
    {
        var cacheKey = $"{PlatformConstants.ThemeProfileCachePrefix}{businessId ?? Guid.Empty}";
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null) return JsonSerializer.Deserialize<ThemeProfileDto>(cached, _json);

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

    // -- ShadCN CSS variables -------------------------------------------------

    public async Task<ShadcnCssVarsDto?> GetShadcnCssVarsAsync(
        Guid? businessId, CancellationToken ct = default)
    {
        var cacheKey = $"{PlatformConstants.ThemeProfileCachePrefix}shadcn:{businessId ?? Guid.Empty}";
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null) return JsonSerializer.Deserialize<ShadcnCssVarsDto>(cached, _json);

        var dto = await GetDefaultAsync(businessId, ct);
        if (dto is null) return null;

        var vars = BuildShadcnCssVars(dto);
        await cache.SetStringAsync(cacheKey,
            JsonSerializer.Serialize(vars, _json),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = PlatformConstants.ThemeCacheTtl },
            ct);
        return vars;
    }

    private static ShadcnCssVarsDto BuildShadcnCssVars(ThemeProfileDto t)
    {
        string primaryDark   = Resolve(t.ColorPrimaryDark,    t.ColorPrimary,    DarkRole.Brand);
        string secondaryDark = Resolve(t.ColorSecondaryDark,  t.ColorSecondary,  DarkRole.Brand);
        string accentDark    = Resolve(t.ColorAccentDark,     t.ColorAccent,     DarkRole.Brand);
        string successDark   = Resolve(t.ColorSuccessDark,    t.ColorSuccess,    DarkRole.Status);
        string warningDark   = Resolve(t.ColorWarningDark,    t.ColorWarning,    DarkRole.Status);
        string dangerDark    = Resolve(t.ColorDangerDark,     t.ColorDanger,     DarkRole.Status);
        string infoDark      = Resolve(t.ColorInfoDark,       t.ColorInfo,       DarkRole.Status);
        string bgDark        = Resolve(t.ColorBackgroundDark, t.ColorBackground, DarkRole.Background);
        string surfaceDark   = Resolve(t.ColorSurfaceDark,    t.ColorSurface,    DarkRole.Surface);
        string borderDark    = Resolve(t.ColorBorderDark,     t.ColorBorder,     DarkRole.Border);
        string textDark      = Resolve(t.ColorTextDark,       t.ColorText,       DarkRole.Text);
        string textMutedDark = Resolve(t.ColorTextMutedDark,  t.ColorTextMuted,  DarkRole.TextMuted);

        string primaryFg     = BestForeground(t.ColorPrimary);
        string destructFg    = BestForeground(t.ColorDanger);
        string primaryFgDark = BestForeground(primaryDark);
        string destructFgDk  = BestForeground(dangerDark);

        static Dictionary<string, string> Vars(
            string bg, string fg, string card, string cardFg,
            string primary, string primaryFg,
            string secondary, string secondaryFg,
            string accent, string accentFg,
            string muted, string mutedFg,
            string destructive, string destructiveFg,
            string border, string input, string ring,
            string success, string warning, string info)
        => new()
        {
            ["--background"]             = ToShadcnHsl(bg),
            ["--foreground"]             = ToShadcnHsl(fg),
            ["--card"]                   = ToShadcnHsl(card),
            ["--card-foreground"]        = ToShadcnHsl(cardFg),
            ["--popover"]                = ToShadcnHsl(card),
            ["--popover-foreground"]     = ToShadcnHsl(cardFg),
            ["--primary"]                = ToShadcnHsl(primary),
            ["--primary-foreground"]     = ToShadcnHsl(primaryFg),
            ["--secondary"]              = ToShadcnHsl(secondary),
            ["--secondary-foreground"]   = ToShadcnHsl(secondaryFg),
            ["--muted"]                  = ToShadcnHsl(muted),
            ["--muted-foreground"]       = ToShadcnHsl(mutedFg),
            ["--accent"]                 = ToShadcnHsl(accent),
            ["--accent-foreground"]      = ToShadcnHsl(accentFg),
            ["--destructive"]            = ToShadcnHsl(destructive),
            ["--destructive-foreground"] = ToShadcnHsl(destructiveFg),
            ["--border"]                 = ToShadcnHsl(border),
            ["--input"]                  = ToShadcnHsl(input),
            ["--ring"]                   = ToShadcnHsl(ring),
            ["--success"]                = ToShadcnHsl(success),
            ["--warning"]                = ToShadcnHsl(warning),
            ["--info"]                   = ToShadcnHsl(info),
        };

        var light = Vars(
            bg: t.ColorBackground, fg: t.ColorText,
            card: t.ColorSurface, cardFg: t.ColorText,
            primary: t.ColorPrimary, primaryFg: primaryFg,
            secondary: t.ColorSurface, secondaryFg: t.ColorText,
            accent: t.ColorSurface, accentFg: t.ColorText,
            muted: t.ColorSurface, mutedFg: t.ColorTextMuted,
            destructive: t.ColorDanger, destructiveFg: destructFg,
            border: t.ColorBorder, input: t.ColorBorder, ring: t.ColorPrimary,
            success: t.ColorSuccess, warning: t.ColorWarning, info: t.ColorInfo
        );

        var dark = Vars(
            bg: bgDark, fg: textDark,
            card: surfaceDark, cardFg: textDark,
            primary: primaryDark, primaryFg: primaryFgDark,
            secondary: surfaceDark, secondaryFg: textDark,
            accent: surfaceDark, accentFg: textDark,
            muted: surfaceDark, mutedFg: textMutedDark,
            destructive: dangerDark, destructiveFg: destructFgDk,
            border: borderDark, input: borderDark, ring: primaryDark,
            success: successDark, warning: warningDark, info: infoDark
        );

        string radius = ToCssRem(t.BorderRadiusMd);

        // Resolve nearest Tailwind colour name for every semantic slot
        var tailwindNames = new Dictionary<string, string>(26)
        {
            // Light palette
            ["primary"]         = TailwindColorPalette.Resolve(t.ColorPrimary),
            ["secondary"]       = TailwindColorPalette.Resolve(t.ColorSecondary),
            ["accent"]          = TailwindColorPalette.Resolve(t.ColorAccent),
            ["success"]         = TailwindColorPalette.Resolve(t.ColorSuccess),
            ["warning"]         = TailwindColorPalette.Resolve(t.ColorWarning),
            ["danger"]          = TailwindColorPalette.Resolve(t.ColorDanger),
            ["info"]            = TailwindColorPalette.Resolve(t.ColorInfo),
            ["background"]      = TailwindColorPalette.Resolve(t.ColorBackground),
            ["surface"]         = TailwindColorPalette.Resolve(t.ColorSurface),
            ["border"]          = TailwindColorPalette.Resolve(t.ColorBorder),
            ["text"]            = TailwindColorPalette.Resolve(t.ColorText),
            ["text-muted"]      = TailwindColorPalette.Resolve(t.ColorTextMuted),
            // Dark palette (resolved or auto-derived)
            ["primary-dark"]    = TailwindColorPalette.Resolve(primaryDark),
            ["secondary-dark"]  = TailwindColorPalette.Resolve(secondaryDark),
            ["accent-dark"]     = TailwindColorPalette.Resolve(accentDark),
            ["success-dark"]    = TailwindColorPalette.Resolve(successDark),
            ["warning-dark"]    = TailwindColorPalette.Resolve(warningDark),
            ["danger-dark"]     = TailwindColorPalette.Resolve(dangerDark),
            ["info-dark"]       = TailwindColorPalette.Resolve(infoDark),
            ["background-dark"] = TailwindColorPalette.Resolve(bgDark),
            ["surface-dark"]    = TailwindColorPalette.Resolve(surfaceDark),
            ["border-dark"]     = TailwindColorPalette.Resolve(borderDark),
            ["text-dark"]       = TailwindColorPalette.Resolve(textDark),
            ["text-muted-dark"] = TailwindColorPalette.Resolve(textMutedDark),
        };

        return new ShadcnCssVarsDto(light, dark, t.FontFamily, t.FontFamilyMono, radius, t.Name, tailwindNames);
    }

    private static string Resolve(string? storedDark, string light, DarkRole role)
        => !string.IsNullOrWhiteSpace(storedDark) ? storedDark : DeriveDark(light, role);

    private static string ToCssRem(string px)
    {
        if (px.EndsWith("px") && double.TryParse(px.Replace("px", ""), out var v))
            return $"{v / 16:F3}rem";
        return px;
    }

    // -- Write ----------------------------------------------------------------

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

        existing.Name              = req.Name;
        existing.Description       = req.Description;
        existing.ColorPrimary      = req.ColorPrimary;
        existing.ColorSecondary    = req.ColorSecondary;
        existing.ColorAccent       = req.ColorAccent;
        existing.ColorSuccess      = req.ColorSuccess;
        existing.ColorWarning      = req.ColorWarning;
        existing.ColorDanger       = req.ColorDanger;
        existing.ColorInfo         = req.ColorInfo;
        existing.ColorBackground   = req.ColorBackground;
        existing.ColorSurface      = req.ColorSurface;
        existing.ColorBorder       = req.ColorBorder;
        existing.ColorText         = req.ColorText;
        existing.ColorTextMuted    = req.ColorTextMuted;
        existing.ColorPrimaryDark  = req.ColorPrimaryDark;
        existing.ColorSecondaryDark  = req.ColorSecondaryDark;
        existing.ColorAccentDark   = req.ColorAccentDark;
        existing.ColorSuccessDark  = req.ColorSuccessDark;
        existing.ColorWarningDark  = req.ColorWarningDark;
        existing.ColorDangerDark   = req.ColorDangerDark;
        existing.ColorInfoDark     = req.ColorInfoDark;
        existing.ColorBackgroundDark = req.ColorBackgroundDark;
        existing.ColorSurfaceDark  = req.ColorSurfaceDark;
        existing.ColorBorderDark   = req.ColorBorderDark;
        existing.ColorTextDark     = req.ColorTextDark;
        existing.ColorTextMutedDark = req.ColorTextMutedDark;

        if (req.ColorPrimaryDark is not null || req.ColorSecondaryDark is not null)
            existing.LogoColorsOverridden = true;

        existing.FontFamily        = req.FontFamily;
        existing.FontFamilyMono    = req.FontFamilyMono;
        existing.FontSizeBase      = req.FontSizeBase;
        existing.FontScaleRatio    = req.FontScaleRatio;
        existing.SpacingUnit       = req.SpacingUnit;
        existing.BorderRadiusSm    = req.BorderRadiusSm;
        existing.BorderRadiusMd    = req.BorderRadiusMd;
        existing.BorderRadiusLg    = req.BorderRadiusLg;
        existing.BorderRadiusFull  = req.BorderRadiusFull;
        existing.ShadowSm          = req.ShadowSm;
        existing.ShadowMd          = req.ShadowMd;
        existing.ShadowLg          = req.ShadowLg;
        existing.SidebarWidth      = req.SidebarWidth;
        existing.TopbarHeight      = req.TopbarHeight;
        existing.ContentMaxWidth   = req.ContentMaxWidth;
        existing.SidebarPosition   = req.SidebarPosition;
        existing.ExtensionTokensJson = req.ExtensionTokensJson;
        existing.ModifiedAtUtc     = DateTimeOffset.UtcNow;

        if (req.SetAsDefault)
        {
            await db.ThemeProfiles
                .Where(t => t.BusinessId == req.BusinessId && t.Id != existing.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDefault, false), ct);
            existing.IsDefault = true;
        }

        await db.SaveChangesAsync(ct);
        await InvalidateCacheAsync(req.BusinessId, ct);
        logger.LogInformation("[Theme] Upserted profile `{Name}` for business {BizId}", req.Name, req.BusinessId);
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
        await InvalidateCacheAsync(profile.BusinessId, ct);
    }

    public async Task DeleteAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await db.ThemeProfiles.FirstOrDefaultAsync(t => t.Id == profileId, ct)
            ?? throw new KeyNotFoundException($"ThemeProfile {profileId} not found.");

        db.ThemeProfiles.Remove(profile);
        await db.SaveChangesAsync(ct);
        await InvalidateCacheAsync(profile.BusinessId, ct);
    }

    // -- Logo colour extraction -----------------------------------------------

    public async Task<ThemeProfileDto> ExtractLogoColorsAsync(
        Guid profileId, ExtractLogoColorsRequest req, CancellationToken ct = default)
    {
        var profile = await db.ThemeProfiles.FirstOrDefaultAsync(t => t.Id == profileId, ct)
            ?? throw new KeyNotFoundException($"ThemeProfile {profileId} not found.");

        var swatches = await logoExtractor.ExtractAsync(req.LogoUrl, maxColors: 5, ct);

        profile.LogoExtractedColorsJson  = JsonSerializer.Serialize(swatches, _json);
        profile.LogoColorsExtractedAtUtc = DateTimeOffset.UtcNow;

        if (!profile.LogoColorsOverridden || req.ForceOverride)
        {
            if (swatches.Count >= 1)
            {
                profile.ColorPrimary = swatches[0].Hex;
                logger.LogInformation("[Theme] Auto-set primary from logo: {Hex} ({Pct}%)",
                    swatches[0].Hex, swatches[0].Percentage);
            }
            if (swatches.Count >= 2)
            {
                profile.ColorSecondary = swatches[1].Hex;
                logger.LogInformation("[Theme] Auto-set secondary from logo: {Hex} ({Pct}%)",
                    swatches[1].Hex, swatches[1].Percentage);
            }
            if (req.ForceOverride) profile.LogoColorsOverridden = false;
        }

        profile.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await InvalidateCacheAsync(profile.BusinessId, ct);
        return profile.ToDto();
    }

    // -- Cache helpers --------------------------------------------------------

    private async Task InvalidateCacheAsync(Guid? businessId, CancellationToken ct)
    {
        var key = $"{PlatformConstants.ThemeProfileCachePrefix}{businessId ?? Guid.Empty}";
        await cache.RemoveAsync(key, ct);
        await cache.RemoveAsync(
            $"{PlatformConstants.ThemeProfileCachePrefix}shadcn:{businessId ?? Guid.Empty}", ct);
    }
}

file static class ThemeMappers
{
    public static ThemeProfileDto ToDto(this ThemeProfile t) => new(
        t.Id, t.BusinessId, t.Name, t.Description, t.IsDefault,
        t.ColorPrimary, t.ColorSecondary, t.ColorAccent,
        t.ColorSuccess, t.ColorWarning, t.ColorDanger, t.ColorInfo,
        t.ColorBackground, t.ColorSurface, t.ColorBorder, t.ColorText, t.ColorTextMuted,
        t.ColorPrimaryDark, t.ColorSecondaryDark, t.ColorAccentDark,
        t.ColorSuccessDark, t.ColorWarningDark, t.ColorDangerDark, t.ColorInfoDark,
        t.ColorBackgroundDark, t.ColorSurfaceDark, t.ColorBorderDark,
        t.ColorTextDark, t.ColorTextMutedDark,
        t.FontFamily, t.FontFamilyMono, t.FontSizeBase, t.FontScaleRatio,
        t.SpacingUnit,
        t.BorderRadiusSm, t.BorderRadiusMd, t.BorderRadiusLg, t.BorderRadiusFull,
        t.ShadowSm, t.ShadowMd, t.ShadowLg,
        t.SidebarWidth, t.TopbarHeight, t.ContentMaxWidth, t.SidebarPosition,
        t.LogoExtractedColorsJson, t.LogoColorsOverridden, t.LogoColorsExtractedAtUtc,
        t.ExtensionTokensJson,
        t.CreatedAtUtc);
}
