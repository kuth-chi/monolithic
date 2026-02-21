namespace Monolithic.Api.Common.Pagination;

/// <summary>
/// Reusable base for all paginated, sortable, and filterable list queries.
/// Bind via <c>[FromQuery]</c> on any controller action that returns a list.
/// Extend this record in each module for domain-specific filter properties.
/// </summary>
public record QueryParameters
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize     = 100;

    private int _size = DefaultPageSize;

    /// <summary>1-based page number. Minimum: 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Items per page. Clamped to [1, 100].
    /// Defaults to 20 when the supplied value is out of range.
    /// </summary>
    public int Size
    {
        get => _size;
        init => _size = value is < 1 or > MaxPageSize ? DefaultPageSize : value;
    }

    /// <summary>Field name to sort by (case-insensitive). Null = default ordering.</summary>
    public string? SortBy { get; init; }

    /// <summary>When true, the sort direction is descending. Default: ascending.</summary>
    public bool SortDesc { get; init; }

    /// <summary>Free-text search term applied against searchable fields.</summary>
    public string? Search { get; init; }

    /// <summary>
    /// Produces a compact, URL-safe string that uniquely identifies this query configuration.
    /// Used as part of the two-level cache key so different query combos get separate entries.
    /// </summary>
    public virtual string ToCacheSegment() =>
        $"p{Math.Max(1, Page)}:s{Size}:sb{SortBy ?? ""}:sd{SortDesc}:q{Search?.ToLowerInvariant() ?? ""}";
}
