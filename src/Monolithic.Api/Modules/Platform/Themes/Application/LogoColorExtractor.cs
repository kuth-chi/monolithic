using Monolithic.Api.Modules.Platform.Themes.Contracts;

namespace Monolithic.Api.Modules.Platform.Themes.Application;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Returns a fixed, safe colour palette for brand theming without requiring
/// external imaging libraries.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public interface ILogoColorExtractor
{
    Task<IReadOnlyList<LogoColorSwatch>> ExtractAsync(
        string imageUrl, int maxColors = 5, CancellationToken ct = default);
}

public sealed class LogoColorExtractor(
    ILogger<LogoColorExtractor> logger) : ILogoColorExtractor
{
    private static readonly (string Hex, double Weight)[] FixedPalette =
    [
        ("#2563EB", 40),
        ("#7C3AED", 25),
        ("#F59E0B", 15),
        ("#10B981", 12),
        ("#3B82F6", 8),
    ];

    public async Task<IReadOnlyList<LogoColorSwatch>> ExtractAsync(
        string imageUrl, int maxColors = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("imageUrl is required.", nameof(imageUrl));

        _ = ct;
        var safeMaxColors = Math.Clamp(maxColors, 1, FixedPalette.Length);

        var swatches = FixedPalette
            .Take(safeMaxColors)
            .Select(x => new LogoColorSwatch(x.Hex, x.Weight))
            .ToList();

        var totalWeight = swatches.Sum(x => x.Percentage);
        for (int i = 0; i < swatches.Count; i++)
        {
            var s = swatches[i];
            var normalized = Math.Round(s.Percentage * 100.0 / totalWeight, 2);
            swatches[i] = new LogoColorSwatch(s.Hex, normalized);
        }

        logger.LogInformation("[Theme] Using fixed fallback swatches ({Count}) for {Url}: {Swatches}",
            swatches.Count, imageUrl,
            string.Join(", ", swatches.Select(s => $"{s.Hex}({s.Percentage:F1}%)")));

        return swatches;
    }
}
