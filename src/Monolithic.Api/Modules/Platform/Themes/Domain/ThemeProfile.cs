using System.Text.Json;

namespace Monolithic.Api.Modules.Platform.Themes.Domain;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Full design-token theme profile for a business.
///
/// A <see cref="ThemeProfile"/> captures every visual token needed by a design
/// system (colours, typography, spacing scale, radius, shadow, layout settings).
/// Stored as individual columns + JSON blobs so the UI can read typed tokens
/// without deserializing the entire JSONB column, but modules can still store
/// arbitrary extension tokens in <see cref="ExtensionTokensJson"/>.
///
/// Multiple profiles per business are supported (A/B testing, dark/light).
/// One profile is marked <see cref="IsDefault"/>.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public class ThemeProfile
{
    public Guid Id { get; set; }

    // ── Ownership ─────────────────────────────────────────────────────────────

    /// <summary>Null = global system theme (fallback for all businesses).</summary>
    public Guid? BusinessId { get; set; }

    public string Name { get; set; } = "Default";

    public string? Description { get; set; }

    /// <summary>Active theme for this business shown to all users.</summary>
    public bool IsDefault { get; set; } = true;

    // ── Brand colours ─────────────────────────────────────────────────────────

    public string ColorPrimary   { get; set; } = "#2563EB";
    public string ColorSecondary { get; set; } = "#7C3AED";
    public string ColorAccent    { get; set; } = "#F59E0B";
    public string ColorSuccess   { get; set; } = "#10B981";
    public string ColorWarning   { get; set; } = "#F59E0B";
    public string ColorDanger    { get; set; } = "#EF4444";
    public string ColorInfo      { get; set; } = "#3B82F6";

    // ── Surface / neutrals ────────────────────────────────────────────────────

    public string ColorBackground { get; set; } = "#FFFFFF";
    public string ColorSurface    { get; set; } = "#F9FAFB";
    public string ColorBorder     { get; set; } = "#E5E7EB";
    public string ColorText       { get; set; } = "#111827";
    public string ColorTextMuted  { get; set; } = "#6B7280";

    // ── Typography ────────────────────────────────────────────────────────────

    public string FontFamily        { get; set; } = "Inter, sans-serif";
    public string FontFamilyMono    { get; set; } = "'JetBrains Mono', monospace";
    public string FontSizeBase      { get; set; } = "16px";
    public decimal FontScaleRatio   { get; set; } = 1.25m;

    // ── Spacing scale ─────────────────────────────────────────────────────────

    /// <summary>Base spacing unit in pixels (e.g. 4 → spacing-1=4px, spacing-2=8px).</summary>
    public int SpacingUnit { get; set; } = 4;

    // ── Shape ─────────────────────────────────────────────────────────────────

    public string BorderRadiusSm { get; set; } = "4px";
    public string BorderRadiusMd { get; set; } = "8px";
    public string BorderRadiusLg { get; set; } = "12px";
    public string BorderRadiusFull { get; set; } = "9999px";

    // ── Shadows ───────────────────────────────────────────────────────────────

    public string ShadowSm  { get; set; } = "0 1px 2px 0 rgb(0 0 0 / 0.05)";
    public string ShadowMd  { get; set; } = "0 4px 6px -1px rgb(0 0 0 / 0.1)";
    public string ShadowLg  { get; set; } = "0 10px 15px -3px rgb(0 0 0 / 0.1)";

    // ── Layout ────────────────────────────────────────────────────────────────

    public string SidebarWidth      { get; set; } = "256px";
    public string TopbarHeight      { get; set; } = "64px";
    public string ContentMaxWidth   { get; set; } = "1280px";

    /// <summary>"left" or "right" sidebar position.</summary>
    public string SidebarPosition   { get; set; } = "left";

    // ── Dark-mode colour overrides ────────────────────────────────────────────
    //    When null the service auto-derives a dark-mode equivalent from the
    //    corresponding light colour.  Setting these explicitly lets brand owners
    //    fine-tune the dark palette without touching the light one.

    public string? ColorPrimaryDark    { get; set; }
    public string? ColorSecondaryDark  { get; set; }
    public string? ColorAccentDark     { get; set; }
    public string? ColorSuccessDark    { get; set; }
    public string? ColorWarningDark    { get; set; }
    public string? ColorDangerDark     { get; set; }
    public string? ColorInfoDark       { get; set; }
    public string? ColorBackgroundDark { get; set; }
    public string? ColorSurfaceDark    { get; set; }
    public string? ColorBorderDark     { get; set; }
    public string? ColorTextDark       { get; set; }
    public string? ColorTextMutedDark  { get; set; }

    // ── Logo-extracted palette ────────────────────────────────────────────────
    //    Populated automatically when the business uploads a logo.
    //    JSON array of { "Hex": "#RRGGBB", "Percentage": 0.0–100.0 }
    //    sorted descending by percentage.

    /// <summary>
    /// Top dominant colours extracted from the brand logo.
    /// JSON: [{ "Hex": "#2563EB", "Percentage": 42.5 }, ...]
    /// </summary>
    public string? LogoExtractedColorsJson { get; set; }

    /// <summary>UTC timestamp the logo extraction last ran.</summary>
    public DateTimeOffset? LogoColorsExtractedAtUtc { get; set; }

    /// <summary>
    /// When true, primary/secondary were manually overridden AFTER auto-extraction
    /// and the extraction service must NOT overwrite them on re-extraction.
    /// </summary>
    public bool LogoColorsOverridden { get; set; } = false;

    // ── Extensions ───────────────────────────────────────────────────────────

    /// <summary>
    /// JSONB column for module-specific or future design tokens.
    /// Deserialize into <see cref="Dictionary{TKey,TValue}"/> for general access.
    /// </summary>
    public string? ExtensionTokensJson { get; set; }

    // ── Audit ─────────────────────────────────────────────────────────────────

    public DateTimeOffset CreatedAtUtc   { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }
}
