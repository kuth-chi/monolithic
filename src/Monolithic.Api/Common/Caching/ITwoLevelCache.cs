namespace Monolithic.Api.Common.Caching;

public interface ITwoLevelCache
{
    /// <summary>
    /// Checks L1 (in-memory) then L2 (Redis/distributed). On a miss the factory is executed
    /// and the result is written to both layers using the <paramref name="ttl"/> for both.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Same as above but with separate TTLs: <paramref name="l1Ttl"/> for in-memory (L1)
    /// and <paramref name="l2Ttl"/> for the distributed cache (L2 / Redis), enabling
    /// shorter hot-path eviction while keeping the Redis entry warm longer.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan l2Ttl,
        TimeSpan l1Ttl,
        CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}