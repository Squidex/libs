// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;

namespace Squidex.Messaging.Implementation.InMemory
{
    public class InMemorySubscriptionStore : IMessagingSubscriptionStore
    {
        private readonly ConcurrentDictionary<(string Group, string Key), Subscription> subscriptions = new ();

        private sealed class Subscription
        {
            public TimeSpan Expires { get; set; }

            public DateTime LastUpdate { get; set; }

            public SerializedObject Value { get; set; }
        }

        public Task<IReadOnlyList<(string Key, SerializedObject Value)>> GetSubscriptionsAsync(string group, DateTime now,
            CancellationToken ct)
        {
            var result = new List<(string Key, SerializedObject Value)>();

            foreach (var (key, subscription) in subscriptions)
            {
                if (key.Group == group && !IsExpired(subscription, now))
                {
                    result.Add((key.Key, subscription.Value));
                }
            }

            return Task.FromResult<IReadOnlyList<(string Key, SerializedObject Value)>>(result);
        }

        public Task SubscribeAsync(string group, string key, SerializedObject value, DateTime now, TimeSpan expiresAfter,
            CancellationToken ct)
        {
            subscriptions.AddOrUpdate((group, key), (key, arg) =>
            {
                return new Subscription { Expires = arg.expiresAfter, LastUpdate = arg.now, Value = arg.value };
            },
            (key, subscription, arg) =>
            {
                subscription.Expires = arg.expiresAfter;
                subscription.Value = arg.value;

                return subscription;
            }, (expiresAfter, now, value));

            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(string group, string key,
            CancellationToken ct)
        {
            subscriptions.TryRemove((group, key), out _);

            return Task.CompletedTask;
        }

        public Task UpdateAliveAsync(string group, string[] keys, DateTime now,
            CancellationToken ct)
        {
            foreach (var (key, subscription) in subscriptions)
            {
                if (key.Group == group && keys.Contains(key.Key))
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
