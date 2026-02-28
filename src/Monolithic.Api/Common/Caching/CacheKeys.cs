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

    // ── System initialisation ─────────────────────────────────────────────────

    /// <summary>
    /// Boolean flag: <c>true</c> when at least one <c>ApplicationUser</c> row exists.
    /// Used by the anonymous /has-users probe so the frontend can redirect new installations
    /// to /signup instead of /login.
    /// Short L1 TTL (30 s) + longer L2 TTL (5 min) — balances freshness on fresh installs
    /// and eliminates hot-path DB probes on busy instances.
    /// </summary>
    public const string SystemHasUsers = "system:has-users:v1";
}