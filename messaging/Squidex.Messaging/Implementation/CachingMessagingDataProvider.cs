// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Squidex.Messaging.Implementation;

public sealed class CachingMessagingDataProvider : IMessagingDataProvider
{
    private readonly IMessagingDataProvider inner;
    private readonly IMemoryCache cache;
    private readonly MessagingOptions options;

    public CachingMessagingDataProvider(IMemoryCache cache, IMessagingDataProvider inner, IOptions<MessagingOptions> options)
    {
        this.options = options.Value;
        this.cache = cache;
        this.inner = inner;
    }

    public async Task<IReadOnlyDictionary<string, T>> GetEntriesAsync<T>(string group,
        CancellationToken ct = default) where T : notnull
    {
        var cacheKey = CacheKey(group);

        return await cache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = options.DataCacheDuration;

            return inner.GetEntriesAsync<T>(group, ct);
        }) ?? throw new InvalidOperationException("Getting null result from cache.");
    }

    public async Task<IAsyncDisposable> StoreAsync<T>(string group, string key, T entry, TimeSpan expiresAfter,
        CancellationToken ct = default) where T : notnull
    {
        var result = await inner.StoreAsync(group, key, entry, expiresAfter, ct);

        cache.Remove(CacheKey(group));
        return result;
    }

    public async Task DeleteAsync(string group, string key,
        CancellationToken ct = default)
    {
        await inner.DeleteAsync(group, key, ct);

        cache.Remove(CacheKey(group));
    }

    public Task UpdateAliveAsync(
        CancellationToken ct = default)
    {
        return inner.UpdateAliveAsync(ct);
    }

    private static string CacheKey(string group)
    {
        return $"Squidex.Messaging.Subcriptions_{group}";
    }
}
