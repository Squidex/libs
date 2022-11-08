// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Caching.Memory;

namespace Squidex.Messaging.Implementation;

public sealed class CachingSubscriptionStore : IMessagingSubscriptionStore
{
    private readonly IMessagingSubscriptionStore inner;
    private readonly IMemoryCache cache;
    private readonly TimeSpan cacheDuration;

    public CachingSubscriptionStore(IMessagingSubscriptionStore inner, IMemoryCache cache, TimeSpan cacheDuration)
    {
        this.inner = inner;
        this.cache = cache;
        this.cacheDuration = cacheDuration;
    }

    public async Task<IReadOnlyList<(string Key, SerializedObject Value, DateTime Expiration)>> GetSubscriptionsAsync(string group,
        CancellationToken ct)
    {
        var cacheKey = CacheKey(group);
        try
        {
            return await cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = cacheDuration;

                return inner.GetSubscriptionsAsync(group, ct);
            });
        }
        catch
        {
            cache.Remove(cacheKey);
            throw;
        }
    }

    public async Task SubscribeManyAsync(SubscribeRequest[] requests,
        CancellationToken ct)
    {
        await inner.SubscribeManyAsync(requests, ct);

        foreach (var group in requests.Select(x => x.Group).Distinct())
        {
            cache.Remove(CacheKey(group));
        }
    }

    public async Task UnsubscribeAsync(string group, string key,
        CancellationToken ct)
    {
        await inner.UnsubscribeAsync(group, key, ct);

        cache.Remove(CacheKey(group));
    }

    public Task CleanupAsync(DateTime now,
        CancellationToken ct)
    {
        return inner.CleanupAsync(now, ct);
    }

    private static string CacheKey(string group)
    {
        return $"Squidex.Messaging.Subcriptions_{group}";
    }
}
