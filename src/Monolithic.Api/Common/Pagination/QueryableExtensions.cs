using Microsoft.EntityFrameworkCore;

namespace Monolithic.Api.Common.Pagination;

/// <summary>
/// EF Core extension methods for applying pagination to an <see cref="IQueryable{T}"/>.
/// Callers are responsible for applying filtering and ordering BEFORE calling these methods.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Executes a COUNT query and a windowed data query, then packages the results into a
    /// <see cref="PagedResult{T}"/> (without navigation URLs — add those via
    /// <see cref="PagedResultExtensions.WithNavigationUrls{T}"/> in the controller layer).
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> source,
        QueryParameters query,
        CancellationToken ct = default)
    {
        var page  = Math.Max(1, query.Page);
        var size  = query.Size;
        var total = await source.CountAsync(ct);
        var data  = await source
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var message = total == 0 ? "No results found." : null;

        return new PagedResult<T>
        {
            Total   = total,
            Page    = page,
            Size    = size,
            Message = message,
            Data    = data
        };
    }
}
