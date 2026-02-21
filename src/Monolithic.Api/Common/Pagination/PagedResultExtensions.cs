using Microsoft.AspNetCore.Http;

namespace Monolithic.Api.Common.Pagination;

/// <summary>
/// ASP.NET Core–aware extension that enriches a <see cref="PagedResult{T}"/> with
/// <c>NextUrl</c> and <c>PreviousUrl</c> derived from the current HTTP request.
/// Kept in the HTTP/controller layer so the domain service stays framework-free.
/// </summary>
public static class PagedResultExtensions
{
    /// <summary>
    /// Returns a new <see cref="PagedResult{T}"/> with absolute navigation URLs injected,
    /// preserving every existing query-string parameter and only replacing <c>page</c>.
    /// </summary>
    public static PagedResult<T> WithNavigationUrls<T>(
        this PagedResult<T> result,
        HttpRequest request)
    {
        // Build a mutable copy of current query string
        var currentQuery = request.Query
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        var hasNext = result.Page * result.Size < result.Total;
        var hasPrev = result.Page > 1;

        return result with
        {
            NextUrl     = hasNext ? BuildAbsoluteUrl(request, currentQuery, result.Page + 1) : null,
            PreviousUrl = hasPrev ? BuildAbsoluteUrl(request, currentQuery, result.Page - 1) : null
        };
    }

    private static string BuildAbsoluteUrl(
        HttpRequest request,
        Dictionary<string, string> query,
        int page)
    {
        var q = new Dictionary<string, string>(query, StringComparer.OrdinalIgnoreCase)
        {
            ["page"] = page.ToString()
        };

        var qs = string.Join("&", q.Select(
            kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return $"{request.Scheme}://{request.Host}{request.Path}?{qs}";
    }
}
