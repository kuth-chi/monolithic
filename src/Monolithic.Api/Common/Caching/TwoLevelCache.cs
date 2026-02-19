using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Monolithic.Api.Common.Caching;

public sealed class TwoLevelCache(IMemoryCache memoryCache, IDistributedCache distributedCache) : ITwoLevelCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (memoryCache.TryGetValue(key, out T? memoryValue) && memoryValue is not null)
        {
            return memoryValue;
        }

        var payload = await distributedCache.GetStringAsync(key, cancellationToken);

        if (!string.IsNullOrWhiteSpace(payload))
        {
            var distributedValue = JsonSerializer.Deserialize<T>(payload, SerializerOptions);

            if (distributedValue is not null)
            {
                memoryCache.Set(key, distributedValue, ttl);
                return distributedValue;
            }
        }

        var created = await factory(cancellationToken);

        memoryCache.Set(key, created, ttl);

        var serialized = JsonSerializer.Serialize(created, SerializerOptions);
        await distributedCache.SetStringAsync(
            key,
            serialized,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);

        return created;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        memoryCache.Remove(key);
        await distributedCache.RemoveAsync(key, cancellationToken);
    }
}