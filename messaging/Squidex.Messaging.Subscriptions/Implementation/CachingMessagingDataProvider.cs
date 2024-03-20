// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using Squidex.Caching;
using Squidex.Messaging.Implementation;

namespace Squidex.Messaging.Subscriptions.Implementation;

public sealed class CachingMessagingDataProvider : IMessagingDataProvider
{
    private readonly IMessagingDataProvider inner;
    private readonly IReplicatedCache cache;
    private readonly MessagingOptions options;

    public CachingMessagingDataProvider(IMessagingDataProvider inner, IReplicatedCache cache, IOptions<MessagingOptions> options)
    {
        this.inner = inner;
        this.cache = cache;
        this.options = options.Value;
    }

    public async Task<IReadOnlyDictionary<string, T>> GetEntriesAsync<T>(string group,
        CancellationToken ct = default) where T : notnull
    {
        var cacheKey = CacheKey(group);

        if (options.SubscriptionCacheDuration > TimeSpan.Zero && cache.TryGetValue(cacheKey, out var c) && c is IReadOnlyDictionary<string, T> result)
        {
            return result;
        }

        result = await inner.GetEntriesAsync<T>(group, ct);

        await cache.AddAsync(cacheKey, result, options.SubscriptionCacheDuration, ct);
        return result;
    }

    public async Task<IAsyncDisposable> StoreAsync<T>(string group, string key, T entry, TimeSpan expiresAfter,
        CancellationToken ct = default) where T : notnull
    {
        var result = await inner.StoreAsync(group, key, entry, expiresAfter, ct);

        if (options.SubscriptionCacheDuration > TimeSpan.Zero)
        {
            await cache.RemoveAsync(CacheKey(group), default);
        }

        return result;
    }

    public async Task DeleteAsync(string group, string key,
        CancellationToken ct = default)
    {
        await inner.DeleteAsync(group, key, ct);

        if (options.SubscriptionCacheDuration > TimeSpan.Zero)
        {
            await cache.RemoveAsync(CacheKey(group), default);
        }
    }

    private static string CacheKey(string group)
    {
        return $"Squidex.Messaging.Subcriptions_{group}";
    }
}
