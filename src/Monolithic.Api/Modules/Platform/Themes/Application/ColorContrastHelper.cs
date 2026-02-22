using System.Globalization;
using System.Text;

namespace Monolithic.Api.Modules.Platform.Themes.Application;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Colour-science utilities used by the theme engine.
///
/// Responsibilities
/// ─────────────────
/// • Hex ↔ RGB ↔ HSL conversions
/// • WCAG AA contrast-ratio calculation
/// • Auto-derivation of dark-mode equivalents from light colours
/// • ShadCN HSL channel string formatting ("H S% L%" without the hsl() wrapper)
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public static class ColorContrastHelper
{
    // ── Parsing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a 3- or 6-digit hex string (with or without #) to (R, G, B).
    /// </summary>
    public static (byte R, byte G, byte B) ParseHex(string hex)
    {
        hex = hex.TrimStart('#').Trim();

        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);

        if (hex.Length != 6)
            throw new ArgumentException($"Invalid hex colour: #{hex}");

        byte r = byte.Parse(hex[..2], NumberStyles.HexNumber);
        byte g = byte.Parse(hex[2..4], NumberStyles.HexNumber);
        byte b = byte.Parse(hex[4..6], NumberStyles.HexNumber);
        return (r, g, b);
    }

    /// <summary>Formats (R, G, B) as "#RRGGBB".</summary>
    public static string ToHex(byte r, byte g, byte b)
        => $"#{r:X2}{g:X2}{b:X2}";

    // ── HSL conversion ────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a hex colour to HSL.
    /// H ∈ [0, 360), S ∈ [0, 100], L ∈ [0, 100].
    /// </summary>
    public static (double H, double S, double L) ToHsl(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        return RgbToHsl(r, g, b);
    }

    public static (double H, double S, double L) RgbToHsl(byte r, byte g, byte b)
    {
        double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
        double max = Math.Max(rf, Math.Max(gf, bf));
        double min = Math.Min(rf, Math.Min(gf, bf));
        double delta = max - min;

        double l = (max + min) / 2.0;
        double s = 0, h = 0;

        if (delta > 1e-10)
        {
            s = delta / (1 - Math.Abs(2 * l - 1));

            if (max == rf)      h = 60 * (((gf - bf) / delta) % 6);
            else if (max == gf) h = 60 * (((bf - rf) / delta) + 2);
            else                h = 60 * (((rf - gf) / delta) + 4);

            if (h < 0) h += 360;
        }

        return (h, s * 100, l * 100);
    }

    public static (byte R, byte G, byte B) HslToRgb(double h, double s, double l)
    {
        s /= 100; l /= 100;
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = l - c / 2;

        double r, g, b;
        if      (h < 60)  { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else              { r = c; g = 0; b = x; }

        return (
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255)
        );
    }

    // ── ShadCN HSL channel string ─────────────────────────────────────────────

    /// <summary>
    /// Returns the ShadCN HSL channel string in format: "H S% L%"
    /// e.g. "221.2 83.2% 53.3%"
    /// Used as CSS custom-property value with <c>hsl(var(--primary))</c>.
    /// </summary>
    public static string ToShadcnHsl(string hex)
    {
        var (h, s, l) = ToHsl(hex);
        return $"{h:F1} {s:F1}% {l:F1}%";
    }

    // ── WCAG contrast ─────────────────────────────────────────────────────────

    private static double LinearizeChannel(double value)
    {
        value /= 255.0;
        return value <= 0.04045
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    /// <summary>Relative luminance per WCAG 2.1 §1.4.3.</summary>
    public static double RelativeLuminance(byte r, byte g, byte b)
        => 0.2126 * LinearizeChannel(r)
         + 0.7152 * LinearizeChannel(g)
         + 0.0722 * LinearizeChannel(b);

    /// <summary>
    /// WCAG contrast ratio between two hex colours.
    /// AA normal text requires ≥ 4.5; large text ≥ 3.0.
    /// </summary>
    public static double ContrastRatio(string hex1, string hex2)
    {
        var (r1, g1, b1) = ParseHex(hex1);
        var (r2, g2, b2) = ParseHex(hex2);
        double l1 = RelativeLuminance(r1, g1, b1);
        double l2 = RelativeLuminance(r2, g2, b2);
        double lighter = Math.Max(l1, l2);
        double darker  = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Returns "#FFFFFF" or "#000000" — whichever has better contrast against <paramref name="bgHex"/>.
    /// </summary>
    public static string BestForeground(string bgHex)
        => ContrastRatio(bgHex, "#FFFFFF") >= ContrastRatio(bgHex, "#000000")
            ? "#FFFFFF"
            : "#000000";

    // ── Dark-mode derivation ──────────────────────────────────────────────────

    /// <summary>
    /// Roles used to choose the correct dark-mode derivation strategy.
    /// </summary>
    public enum DarkRole
    {
        /// <summary>Background canvas (very light → very dark, near-black).</summary>
        Background,
        /// <summary>Card / panel surface (slightly off-white → slightly off-black).</summary>
        Surface,
        /// <summary>Primary body text (near-black → near-white).</summary>
        Text,
        /// <summary>Muted / secondary text (medium grey → lighter grey).</summary>
        TextMuted,
        /// <summary>Dividers and input borders (light grey → dark grey).</summary>
        Border,
        /// <summary>Brand / interactive colour (adjust lightness for dark bg readability).</summary>
        Brand,
        /// <summary>Semantic status colour (success, warning, danger, info).</summary>
        Status,
    }

    /// <summary>
    /// Derives a dark-mode colour from its light counterpart using HSL manipulation.
    /// The result passes WCAG AA against a dark background on a best-effort basis.
    /// </summary>
    public static string DeriveDark(string lightHex, DarkRole role)
    {
        var (h, s, l) = ToHsl(lightHex);

        (double newH, double newS, double newL) = role switch
        {
            // Near-black: keep hue hint, crush lightness
            DarkRole.Background => (h, Math.Min(s, 10), l > 50 ? 8 : l),

            // Dark card surface — slightly lighter than background
            DarkRole.Surface    => (h, Math.Min(s, 15), l > 80 ? 14 : l),

            // Invert text lightness: dark text becomes very light
            DarkRole.Text       => (h, s * 0.6, l < 30 ? 93 : l),

            // Muted text: moderate shift toward lighter
            DarkRole.TextMuted  => (h, s, l < 50 ? l + 25 : l),

            // Borders: light grey → noticeable but dark border
            DarkRole.Border     => (h, Math.Min(s, 20), l > 70 ? 24 : l),

            // Brand colours: if too dark for dark bg, boost lightness
            DarkRole.Brand or DarkRole.Status =>
                l < 50
                    ? (h, Math.Min(s + 5, 90), Math.Min(l + 20, 70))
                    : (h, s, Math.Clamp(l, 55, 75)),

            _ => (h, s, l)
        };

        var (r, g, b) = HslToRgb(newH, newS, newL);
        return ToHex(r, g, b);
    }

    // ── Hex sanitiser ─────────────────────────────────────────────────────────

    /// <summary>
    /// Validates and normalises a hex string.
    /// Throws <see cref="ArgumentException"/> if the value is invalid.
    /// </summary>
    public static string Sanitize(string? hex, string fallback = "#000000")
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try
        {
            var (r, g, b) = ParseHex(hex);
            return ToHex(r, g, b);
        }
        catch
        {
            return fallback;
        }
    }
}
