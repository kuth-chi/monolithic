using Microsoft.Extensions.Options;
using Monolithic.Api.Common.Caching;
using Monolithic.Api.Common.Configuration;
using Monolithic.Api.Modules.Analytics.Contracts;
using Monolithic.Api.Modules.Users.Application;

namespace Monolithic.Api.Modules.Analytics.Application;

public sealed class InMemoryDashboardQueryService(
    IUserService userService,
    ITwoLevelCache cache,
    IOptions<CacheOptions> cacheOptions) : IDashboardQueryService
{
    public async Task<RealtimeDashboardSnapshotDto> GetRealtimeSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var ttlSeconds = Math.Max(5, cacheOptions.Value.DashboardSnapshotSeconds);
        var ttl = TimeSpan.FromSeconds(ttlSeconds);

        return await cache.GetOrCreateAsync(
            CacheKeys.DashboardRealtimeSnapshotV1,
            async ct =>
            {
                var activeUsers = await userService.GetCountAsync(ct);

                return new RealtimeDashboardSnapshotDto
                {
                    GeneratedAtUtc = DateTimeOffset.UtcNow,
                    ActiveUsers = activeUsers,
                    TodaySalesAmount = activeUsers * 120m,
                    LowStockItems = Math.Max(2, 20 - activeUsers),
                    PendingPurchaseOrders = Math.Max(1, activeUsers / 2),
                    MonthlyRevenueAmount = activeUsers * 2500m
                };
            },
            ttl,
            cancellationToken);
    }
}