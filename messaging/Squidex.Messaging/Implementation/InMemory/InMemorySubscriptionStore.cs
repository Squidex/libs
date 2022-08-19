// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;

namespace Squidex.Messaging.Implementation.InMemory
{
    public class InMemorySubscriptionStore : ISubscriptionStore
    {
        private readonly ConcurrentDictionary<(string Topic, string Queue), Subscription> subscriptions = new ();

        private sealed class Subscription
        {
            public TimeSpan Expires { get; set; }

            public DateTime LastUpdate { get; set; }
        }

        public Task<IReadOnlyList<string>> GetSubscriptionsAsync(string topic, DateTime now,
            CancellationToken ct)
        {
            var result = new List<string>();

            foreach (var (key, subscription) in subscriptions)
            {
                if (key.Topic == topic && !IsExpired(subscription, now))
                {
                    result.Add(key.Queue);
                }
            }

            return Task.FromResult<IReadOnlyList<string>>(result);
        }

        public Task SubscribeAsync(string topic, string queue, DateTime now, TimeSpan expiresAfter,
            CancellationToken ct)
        {
            subscriptions.AddOrUpdate((topic, queue), (key, arg) =>
            {
                return new Subscription { Expires = arg.expiresAfter, LastUpdate = arg.now };
            },
            (key, subscription, arg) =>
            {
                subscription.Expires = arg.expiresAfter;

                return subscription;
            }, (expiresAfter, now));

            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(string topic, string queue,
            CancellationToken ct)
        {
            subscriptions.TryRemove((topic, queue), out _);

            return Task.CompletedTask;
        }

        public Task UpdateAliveAsync(string[] queues, DateTime now,
            CancellationToken ct)
        {
            foreach (var (key, subscription) in subscriptions)
            {
                if (queues.Contains(key.Queue))
                {
                    subscription.LastUpdate = now;
                }
            }

            return Task.CompletedTask;
        }

        public Task CleanupAsync(DateTime now,
            CancellationToken ct)
        {
            foreach (var (key, subscription) in subscriptions.ToList())
            {
                if (IsExpired(subscription, now))
                {
                    subscriptions.TryRemove(key, out _);
                }
            }

            return Task.CompletedTask;
        }

        private static bool IsExpired(Subscription subscription, DateTime now)
        {
            return subscription.Expires > TimeSpan.Zero && subscription.LastUpdate + subscription.Expires < now;
        }
    }
}
