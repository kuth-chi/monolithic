namespace Monolithic.Api.Common.Caching;

public static class CacheKeys
{
    // ── Analytics ─────────────────────────────────────────────────────────────
    public const string DashboardRealtimeSnapshotV1 = "dashboard:realtime:v1";

    // ── Business Branches ─────────────────────────────────────────────────────

    /// <summary>Key for a paginated / filtered branch list query.</summary>
    public static string Branches(Guid businessId, string cacheSegment)
        => $"branches:{businessId}:{cacheSegment}";

    /// <summary>Key for a single branch detail lookup.</summary>
    public static string BranchById(Guid branchId)
        => $"branch:{branchId}";

    /// <summary>Key for a paginated / filtered branch-employee list query.</summary>
    public static string BranchEmployees(Guid branchId, string cacheSegment)
        => $"branch-employees:{branchId}:{cacheSegment}";
}