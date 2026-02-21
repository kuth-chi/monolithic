namespace Monolithic.Api.Common.Pagination;

/// <summary>
/// Universal paginated response envelope — the single place that defines the pagination shape
/// for every list endpoint in the application (DRY / reusable).
/// </summary>
/// <typeparam name="T">The item type in the list.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>HTTP-style short status string — "success" or "error".</summary>
    public string Status { get; init; } = "success";

    /// <summary>Optional human-readable message (e.g. "0 results found").</summary>
    public string? Message { get; init; }

    /// <summary>Total rows matching the query (before pagination).</summary>
    public long Total { get; init; }

    /// <summary>Current 1-based page number.</summary>
    public int Page { get; init; }

    /// <summary>Requested page size.</summary>
    public int Size { get; init; }

    /// <summary>Computed total number of pages.</summary>
    public int TotalPages => Size > 0 ? (int)Math.Ceiling((double)Total / Size) : 0;

    /// <summary>Absolute URL of the next page — null when on the last page.</summary>
    public string? NextUrl { get; init; }

    /// <summary>Absolute URL of the previous page — null when on the first page.</summary>
    public string? PreviousUrl { get; init; }

    /// <summary>The items for the current page.</summary>
    public IReadOnlyList<T> Data { get; init; } = [];
}
