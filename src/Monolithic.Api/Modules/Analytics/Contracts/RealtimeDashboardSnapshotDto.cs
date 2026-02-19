namespace Monolithic.Api.Modules.Analytics.Contracts;

public sealed class RealtimeDashboardSnapshotDto
{
    public DateTimeOffset GeneratedAtUtc { get; init; }

    public int ActiveUsers { get; init; }

    public decimal TodaySalesAmount { get; init; }

    public int LowStockItems { get; init; }

    public int PendingPurchaseOrders { get; init; }

    public decimal MonthlyRevenueAmount { get; init; }
}