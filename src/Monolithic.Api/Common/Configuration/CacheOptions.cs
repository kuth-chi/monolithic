namespace Monolithic.Api.Common.Configuration;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int DashboardSnapshotSeconds { get; init; } = 15;
}