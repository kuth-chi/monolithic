using Monolithic.Api.Modules.Analytics.Contracts;

namespace Monolithic.Api.Modules.Analytics.Application;

public interface IDashboardQueryService
{
    Task<RealtimeDashboardSnapshotDto> GetRealtimeSnapshotAsync(CancellationToken cancellationToken = default);
}