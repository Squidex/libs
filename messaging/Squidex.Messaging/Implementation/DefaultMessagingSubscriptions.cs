// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting;
using Squidex.Messaging.Internal;

namespace Squidex.Messaging.Implementation
{
    public sealed class DefaultMessagingSubscriptions : IMessagingSubscriptions, IBackgroundProcess
    {
        private readonly Dictionary<(string Group, string Key), (SerializedObject Value, TimeSpan Expires)> localSubscriptions = new ();
        private readonly MessagingOptions options;
        private readonly IMessagingSubscriptionStore messagingSubscriptionStore;
        private readonly IMessagingSerializer messagingSerializer;
        private readonly ILogger<DefaultMessagingSubscriptions> log;
        private readonly IClock clock;
        private SimpleTimer? updateTimer;
        private SimpleTimer? cleanupTimer;

        public DefaultMessagingSubscriptions(
            IMessagingSubscriptionStore messagingSubscriptionStore,
            IMessagingSerializer messagingSerializer,
            IOptions<MessagingOptions> options,
            ILogger<DefaultMessagingSubscriptions> log, IClock clock)
        {
            this.options = options.Value;
            this.messagingSerializer = messagingSerializer;
            this.messagingSubscriptionStore = messagingSubscriptionStore;
            this.clock = clock;
            this.log = log;
        }

        public Task StartAsync(
            CancellationToken ct)
        {
            // The timer will do the logging anyway, so there is no need to handle exceptions here.
            updateTimer ??= new SimpleTimer(async ct =>
            {
                await UpdateAliveAsync(ct);
            }, options.SubscriptionUpdateInterval, log);

            cleanupTimer ??= new SimpleTimer(async ct =>
            {
                await CleanupAsync(ct);
            }, options.SubscriptionUpdateInterval, log);

            return Task.CompletedTask;
        }

        public Task UpdateAliveAsync(
            CancellationToken ct)
        {
            KeyValuePair<(string Group, string Key), (SerializedObject Value, TimeSpan Expires)>[] subscriptions;

            lock (localSubscriptions)
            {
                subscriptions = localSubscriptions.ToArray();
            }

            if (subscriptions.Length == 0)
            {
                return Task.CompletedTask;
            }

            var now = clock.UtcNow;

            var requests =
                subscriptions
                    .Select(x =>
                        new SubscribeRequest(
                            x.Key.Group,
                            x.Key.Key,
                            x.Value.Value,
                            CalculateExpiration(now, x.Value.Expires)))
                        .ToArray();

            return messagingSubscriptionStore.SubscribeManyAsync(requests, ct);
        }

        public Task CleanupAsync(
            CancellationToken ct)
        {
            return messagingSubscriptionStore.CleanupAsync(clock.UtcNow, ct);
        }

        public async Task StopAsync(
            CancellationToken ct)
        {
            if (updateTimer != null)
            {
                await updateTimer.DisposeAsync();

                updateTimer = null;
            }

            if (cleanupTimer != null)
            {
                await cleanupTimer.DisposeAsync();

                cleanupTimer = null;
            }
        }

        public async Task<IReadOnlyDictionary<string, T>> GetSubscriptionsAsync<T>(string group,
            CancellationToken ct = default) where T : notnull
        {
            var result = new Dictionary<string, T>();

            var now = clock.UtcNow;

            foreach (var (key, value, expiration) in await messagingSubscriptionStore.GetSubscriptionsAsync(group, ct))
            {
                if (expiration < now)
                {
                    continue;
                }

                var deserialized = messagingSerializer.Deserialize(value);

                if (deserialized.Message is T typed)
                {
                    result[key] = typed;
                }
            }

            return result;
        }

        public async Task<IAsyncDisposable> SubscribeAsync<T>(string group, string key, T value, TimeSpan expiresAfter,
            CancellationToken ct = default) where T : notnull
        {
            var serialized = messagingSerializer.Serialize(value);

            lock (localSubscriptions)
            {
                // Store complete subscriptions as local copy to update them periodically. Otherwise we could loose information in race conditions.
                localSubscriptions[(group, key)] = (serialized, expiresAfter);
            }

            var request = new SubscribeRequest(group, key, serialized, CalculateExpiration(clock.UtcNow, expiresAfter));

            await messagingSubscriptionStore.SubscribeManyAsync(new[] { request }, ct);

            return new DelegateAsyncDisposable(() =>
            {
                return new ValueTask(UnsubscribeAsync(group, key, default));
            });
        }

        public Task UnsubscribeAsync(string group, string key,
            CancellationToken ct = default)
        {
            lock (localSubscriptions)
            {
                localSubscriptions.Remove((group, key));
            }

            return messagingSubscriptionStore.UnsubscribeAsync(group, key, ct);
        }

        private static DateTime CalculateExpiration(DateTime now, TimeSpan expires)
        {
            if (expires <= TimeSpan.Zero)
            {
                return DateTime.MaxValue;
            }

            return now + expires;
        }
    }
}
