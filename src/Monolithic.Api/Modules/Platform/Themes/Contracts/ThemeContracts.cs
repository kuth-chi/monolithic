using Monolithic.Api.Modules.Platform.Themes.Domain;

namespace Monolithic.Api.Modules.Platform.Themes.Contracts;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record ThemeProfileDto(
    Guid Id,
    Guid? BusinessId,
    string Name,
    string? Description,
    bool IsDefault,
    // Brand colours – light
    string ColorPrimary, string ColorSecondary, string ColorAccent,
    string ColorSuccess, string ColorWarning, string ColorDanger, string ColorInfo,
    // Surface – light
    string ColorBackground, string ColorSurface, string ColorBorder,
    string ColorText, string ColorTextMuted,
    // Brand colours – dark (nullable = auto-derived by service)
    string? ColorPrimaryDark, string? ColorSecondaryDark, string? ColorAccentDark,
    string? ColorSuccessDark, string? ColorWarningDark, string? ColorDangerDark, string? ColorInfoDark,
    // Surface – dark
    string? ColorBackgroundDark, string? ColorSurfaceDark, string? ColorBorderDark,
    string? ColorTextDark, string? ColorTextMutedDark,
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
    // Logo palette
    string? LogoExtractedColorsJson,
    bool LogoColorsOverridden,
    DateTimeOffset? LogoColorsExtractedAtUtc,
    string? ExtensionTokensJson,
    DateTimeOffset CreatedAtUtc
);

/// <summary>
/// Extracted dominant colour swatch from a logo.
/// </summary>
public sealed record LogoColorSwatch(string Hex, double Percentage);

/// <summary>
/// ShadCN/Tailwind-compatible CSS variable map.
/// Light and dark dictionaries contain HSL channel values (e.g. "221.2 83.2% 53.3%")
/// ready for use as <c>hsl(var(--primary))</c> in CSS.
/// Additional design tokens (radius, font, etc.) are included at the root.
/// </summary>
public sealed record ShadcnCssVarsDto(
    /// <summary>Key = CSS variable name (e.g. "--primary"), value = HSL channels string.</summary>
    IReadOnlyDictionary<string, string> Light,
    /// <summary>Dark-mode CSS variable overrides.</summary>
    IReadOnlyDictionary<string, string> Dark,
    /// <summary>Font family string.</summary>
    string FontFamily,
    /// <summary>Monospace font-family string.</summary>
    string FontFamilyMono,
    /// <summary>Base border-radius token (e.g. "0.5rem").</summary>
    string Radius,
    /// <summary>ISO name of the source theme profile.</summary>
    string ProfileName,
    /// <summary>
    /// Nearest Tailwind CSS v3 colour names for each semantic role.
    /// Keys use the semantic slot name (e.g. <c>"primary"</c>, <c>"danger"</c>,
    /// <c>"background"</c>, <c>"primary-dark"</c>) and values are Tailwind
    /// colour identifiers (e.g. <c>"blue-600"</c>) ready for use as CSS class
    /// prefixes: <c>bg-blue-600</c>, <c>text-blue-600</c>, <c>border-blue-600</c>.
    /// </summary>
    IReadOnlyDictionary<string, string> TailwindColorNames
);

// ── Requests ─────────────────────────────────────────────────────────────────

public sealed record UpsertThemeProfileRequest(
    Guid? BusinessId,
    string Name,
    string? Description,
    // Light colours
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
    // Dark colours (null = auto-derive on read)
    string? ColorPrimaryDark    = null,
    string? ColorSecondaryDark  = null,
    string? ColorAccentDark     = null,
    string? ColorSuccessDark    = null,
    string? ColorWarningDark    = null,
    string? ColorDangerDark     = null,
    string? ColorInfoDark       = null,
    string? ColorBackgroundDark = null,
    string? ColorSurfaceDark    = null,
    string? ColorBorderDark     = null,
    string? ColorTextDark       = null,
    string? ColorTextMutedDark  = null,
    // Typography
    string FontFamily       = "Inter, sans-serif",
    string FontFamilyMono   = "'JetBrains Mono', monospace",
    string FontSizeBase     = "16px",
    decimal FontScaleRatio  = 1.25m,
    // Spacing
    int SpacingUnit = 4,
    // Shape
    string BorderRadiusSm   = "4px",
    string BorderRadiusMd   = "8px",
    string BorderRadiusLg   = "12px",
    string BorderRadiusFull = "9999px",
    // Shadow
    string ShadowSm = "0 1px 2px 0 rgb(0 0 0 / 0.05)",
    string ShadowMd = "0 4px 6px -1px rgb(0 0 0 / 0.1)",
    string ShadowLg = "0 10px 15px -3px rgb(0 0 0 / 0.1)",
    // Layout
    string SidebarWidth    = "256px",
    string TopbarHeight    = "64px",
    string ContentMaxWidth = "1280px",
    string SidebarPosition = "left",
    string? ExtensionTokensJson = null,
    bool SetAsDefault = true
);

/// <summary>Request body for the logo-colour-extraction endpoint.</summary>
public sealed record ExtractLogoColorsRequest(
    /// <summary>Public URL of the logo image to analyse.</summary>
    string LogoUrl,
    /// <summary>
    /// When true, extracted colours overwrite ColorPrimary/ColorSecondary even if
    /// <see cref="ThemeProfile.LogoColorsOverridden"/> is set.
    /// </summary>
    bool ForceOverride = false
);
