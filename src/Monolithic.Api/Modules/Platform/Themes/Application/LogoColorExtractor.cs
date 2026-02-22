using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Monolithic.Api.Modules.Platform.Themes.Contracts;

namespace Monolithic.Api.Modules.Platform.Themes.Application;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Downloads a brand logo image and extracts the dominant colour swatches.
///
/// Algorithm
/// ─────────
/// 1. Download the image via <see cref="HttpClient"/>.
/// 2. Resize to ≤ 200 × 200 pixels (speed — logo details aren't needed).
/// 3. Sample every pixel, skipping near-transparent ones (alpha &lt; 30).
/// 4. Quantize each pixel to a 32-step colour cube (buckets of 8 per channel).
/// 5. Sort buckets by count descending and return the top <paramref name="maxColors"/> swatches.
///
/// The first swatch (highest %) maps to <c>ColorPrimary</c>;
/// the second maps to <c>ColorSecondary</c> if present.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public interface ILogoColorExtractor
{
    Task<IReadOnlyList<LogoColorSwatch>> ExtractAsync(
        string imageUrl, int maxColors = 5, CancellationToken ct = default);
}

public sealed class LogoColorExtractor(
    IHttpClientFactory httpClientFactory,
    ILogger<LogoColorExtractor> logger) : ILogoColorExtractor
{
    private const int MaxSidePixels = 200;
    private const int BucketStep    = 8;   // 256 / 8 = 32 buckets per channel
    private const int AlphaThreshold = 30; // ignore near-transparent

    public async Task<IReadOnlyList<LogoColorSwatch>> ExtractAsync(
        string imageUrl, int maxColors = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("imageUrl is required.", nameof(imageUrl));

        // ── 1. Download ───────────────────────────────────────────────────────
        using var http   = httpClientFactory.CreateClient("logo-extractor");
        using var stream = await http.GetStreamAsync(imageUrl, ct);

        // ── 2. Resize ─────────────────────────────────────────────────────────
        using var image = await Image.LoadAsync<Rgba32>(stream, ct);
        if (image.Width > MaxSidePixels || image.Height > MaxSidePixels)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(MaxSidePixels, MaxSidePixels),
                Mode = ResizeMode.Max,
            }));
        }

        // ── 3 & 4. Sample + quantize ──────────────────────────────────────────
        var histogram = new Dictionary<(byte, byte, byte), int>();

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    var px = row[x];
                    if (px.A < AlphaThreshold) continue;

                    // Bucket each channel to nearest multiple of BucketStep
                    byte br = Quantize(px.R);
                    byte bg = Quantize(px.G);
                    byte bb = Quantize(px.B);

                    var key = (br, bg, bb);
                    histogram.TryGetValue(key, out int count);
                    histogram[key] = count + 1;
                }
            }
        });

        if (histogram.Count == 0)
        {
            logger.LogWarning("[Theme] Logo colour extraction: no opaque pixels found in {Url}", imageUrl);
            return [];
        }

        // ── 5. Sort & return ──────────────────────────────────────────────────
        int totalPixels = histogram.Values.Sum();

        var swatches = histogram
            .OrderByDescending(kv => kv.Value)
            .Take(maxColors)
            .Select(kv =>
            {
                var (r, g, b) = kv.Key;
                double pct = kv.Value * 100.0 / totalPixels;
                string hex = ColorContrastHelper.ToHex(r, g, b);
                return new LogoColorSwatch(hex, Math.Round(pct, 2));
            })
            .ToList();

        logger.LogInformation("[Theme] Extracted {Count} swatches from {Url}: {Swatches}",
            swatches.Count, imageUrl,
            string.Join(", ", swatches.Select(s => $"{s.Hex}({s.Percentage:F1}%)")));

        return swatches;
    }

    private static byte Quantize(byte channel)
    {
        int bucket = (channel / BucketStep) * BucketStep;
        return (byte)Math.Min(bucket, 255);
    }
}
