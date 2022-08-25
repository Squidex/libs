// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Caching.Memory;

namespace Squidex.Messaging.Implementation
{
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

        public async Task<IReadOnlyList<(string Key, SerializedObject Value)>> GetSubscriptionsAsync(string group, DateTime now,
            CancellationToken ct)
        {
            var cacheKey = CacheKey(group);
            try
            {
                return await cache.GetOrCreate(cacheKey, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = cacheDuration;

                    return inner.GetSubscriptionsAsync(group, now, ct);
                });
            }
            catch
            {
                cache.Remove(cacheKey);
                throw;
            }
        }

        public async Task SubscribeAsync(string group, string key, SerializedObject value, DateTime now, TimeSpan expiresAfter,
            CancellationToken ct)
        {
            await inner.SubscribeAsync(group, key, value, now, expiresAfter, ct);

            cache.Remove(CacheKey(group));
        }

        public async Task UnsubscribeAsync(string group, string key,
            CancellationToken ct)
        {
            await inner.UnsubscribeAsync(group, key, ct);

            cache.Remove(CacheKey(group));
        }

        public Task UpdateAliveAsync(string group, string[] keys, DateTime now,
            CancellationToken ct)
        {
            return inner.UpdateAliveAsync(group, keys, now, ct);
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
}
