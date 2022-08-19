// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Caching.Memory;

namespace Squidex.Messaging.Implementation
{
    public sealed class CachingSubscriptionStore : ISubscriptionStore
    {
        private readonly ISubscriptionStore inner;
        private readonly IMemoryCache cache;
        private readonly TimeSpan cacheDuration;

        public CachingSubscriptionStore(ISubscriptionStore inner, IMemoryCache cache, TimeSpan cacheDuration)
        {
            this.inner = inner;
            this.cache = cache;
            this.cacheDuration = cacheDuration;
        }

        public async Task<IReadOnlyList<string>> GetSubscriptionsAsync(string topic, DateTime now,
            CancellationToken ct)
        {
            var cacheKey = CacheKey(topic);
            try
            {
                return await cache.GetOrCreate(cacheKey, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = cacheDuration;

                    return inner.GetSubscriptionsAsync(topic, now, ct);
                });
            }
            catch
            {
                cache.Remove(cacheKey);
                throw;
            }
        }

        public async Task SubscribeAsync(string topic, string queue, DateTime now, TimeSpan expiresAfter,
            CancellationToken ct)
        {
            await inner.SubscribeAsync(topic, queue, now, expiresAfter, ct);

            cache.Remove(CacheKey(topic));
        }

        public async Task UnsubscribeAsync(string topic, string queue,
            CancellationToken ct)
        {
            await inner.UnsubscribeAsync(topic, queue, ct);

            cache.Remove(CacheKey(topic));
        }

        public Task UpdateAliveAsync(string[] queues, DateTime now,
            CancellationToken ct)
        {
            return inner.UpdateAliveAsync(queues, now, ct);
        }

        public Task CleanupAsync(DateTime now,
            CancellationToken ct)
        {
            return inner.CleanupAsync(now, ct);
        }

        private static string CacheKey(string topic)
        {
            return $"Squidex.Messaging.Subcriptions_{topic}";
        }
    }
}
