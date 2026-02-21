using Monolithic.Api.Modules.Platform.Themes.Domain;

namespace Monolithic.Api.Modules.Platform.Themes.Contracts;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record ThemeProfileDto(
    Guid Id,
    Guid? BusinessId,
    string Name,
    string? Description,
    bool IsDefault,
    // Brand colours
    string ColorPrimary, string ColorSecondary, string ColorAccent,
    string ColorSuccess, string ColorWarning, string ColorDanger, string ColorInfo,
    // Surface
    string ColorBackground, string ColorSurface, string ColorBorder,
    string ColorText, string ColorTextMuted,
    // Typography
    string FontFamily, string FontFamilyMono, string FontSizeBase, decimal FontScaleRatio,
    // Spacing
    int SpacingUnit,
    // Shape
    string BorderRadiusSm, string BorderRadiusMd, string BorderRadiusLg, string BorderRadiusFull,
    // Shadow
    string ShadowSm, string ShadowMd, string ShadowLg,
    // Layout
    string SidebarWidth, string TopbarHeight, string ContentMaxWidth, string SidebarPosition,
    string? ExtensionTokensJson,
    DateTimeOffset CreatedAtUtc
);

// ── Requests ─────────────────────────────────────────────────────────────────

public sealed record UpsertThemeProfileRequest(
    Guid? BusinessId,
    string Name,
    string? Description,
    string ColorPrimary,
    string ColorSecondary,
    string ColorAccent,
    string ColorSuccess,
    string ColorWarning,
    string ColorDanger,
    string ColorInfo,
    string ColorBackground,
    string ColorSurface,
    string ColorBorder,
    string ColorText,
    string ColorTextMuted,
    string FontFamily,
    string FontFamilyMono,
    string FontSizeBase,
    decimal FontScaleRatio,
    int SpacingUnit,
    string BorderRadiusSm,
    string BorderRadiusMd,
    string BorderRadiusLg,
    string BorderRadiusFull,
    string ShadowSm,
    string ShadowMd,
    string ShadowLg,
    string SidebarWidth,
    string TopbarHeight,
    string ContentMaxWidth,
    string SidebarPosition,
    string? ExtensionTokensJson,
    bool SetAsDefault = true
);
