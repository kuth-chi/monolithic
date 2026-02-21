using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Monolithic.Api.Common.Caching;

public sealed class TwoLevelCache(IMemoryCache memoryCache, IDistributedCache distributedCache) : ITwoLevelCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    // ── Public overloads (single TTL and explicit dual-TTL) ───────────────────

    /// <inheritdoc/>
    public Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
        where T : class
        => GetOrCreateCoreAsync(key, factory, l2Ttl: ttl, l1Ttl: ttl, cancellationToken);

    /// <inheritdoc/>
    public Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan l2Ttl,
        TimeSpan l1Ttl,
        CancellationToken cancellationToken = default)
        where T : class
        => GetOrCreateCoreAsync(key, factory, l2Ttl, l1Ttl, cancellationToken);

    // ── Core implementation ───────────────────────────────────────────────────

    private async Task<T> GetOrCreateCoreAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan l2Ttl,
        TimeSpan l1Ttl,
        CancellationToken cancellationToken)
        where T : class
    {
        // L1 — in-memory (fastest, process-local)
        if (memoryCache.TryGetValue(key, out T? memoryValue) && memoryValue is not null)
            return memoryValue;

        // L2 — distributed (Redis in production, in-memory stub in development)
        var payload = await distributedCache.GetStringAsync(key, cancellationToken);

        if (!string.IsNullOrWhiteSpace(payload))
        {
            var distributedValue = JsonSerializer.Deserialize<T>(payload, SerializerOptions);

            if (distributedValue is not null)
            {
                memoryCache.Set(key, distributedValue, l1Ttl); // backfill L1
                return distributedValue;
            }
        }

        // Cache miss — call factory, write to both layers
        var created = await factory(cancellationToken);

        memoryCache.Set(key, created, l1Ttl);

        var serialized = JsonSerializer.Serialize(created, SerializerOptions);
        await distributedCache.SetStringAsync(
            key,
            serialized,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = l2Ttl },
            cancellationToken);

        return created;
    }

    // ── Eviction ──────────────────────────────────────────────────────────────

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        memoryCache.Remove(key);
        await distributedCache.RemoveAsync(key, cancellationToken);
    }
}