namespace Monolithic.Api.Common.BackgroundServices;

/// <summary>
/// Configuration options for the <see cref="SoftDeletePurgeService"/>.
/// Bind from <c>appsettings.json</c> under the key <c>"SoftDeletePurge"</c>.
/// </summary>
public sealed class SoftDeletePurgeOptions
{
    public const string SectionName = "SoftDeletePurge";

    /// <summary>
    /// Platform-wide default retention in days, applied when a business has no
    /// <c>BusinessSetting.SoftDeleteRetentionDays</c> configured.
    /// Default: 30 days.
    /// </summary>
    public int SystemDefaultRetentionDays { get; set; } = 30;

    /// <summary>
    /// How often the purge runner executes, in hours.
    /// Default: 24 hours (once per day).
    /// </summary>
    public double RunIntervalHours { get; set; } = 24;
}
