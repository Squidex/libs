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
        private readonly Dictionary<string, HashSet<string>> localSubscriptions = new ();
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

        public async Task UpdateAliveAsync(
            CancellationToken ct)
        {
            Dictionary<string, string[]>? targets = null;

            lock (localSubscriptions)
            {
                foreach (var (group, keys) in localSubscriptions.Where(x => x.Value.Count > 0))
                {
                    targets ??= new Dictionary<string, string[]>();
                    targets[group] = keys.ToArray();
                }
            }

            if (targets == null)
            {
                return;
            }

            var now = clock.UtcNow;

            foreach (var (group, keys) in targets)
            {
                try
                {
                    await messagingSubscriptionStore.UpdateAliveAsync(group, keys, clock.UtcNow, ct);
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Failed to update alive for {grou}.", group);
                }
            }
        }

        public async Task CleanupAsync(
            CancellationToken ct)
        {
            await messagingSubscriptionStore.CleanupAsync(clock.UtcNow, ct);
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

            foreach (var (key, value) in await messagingSubscriptionStore.GetSubscriptionsAsync(group, clock.UtcNow, ct))
            {
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
            lock (localSubscriptions)
            {
                localSubscriptions.GetOrAddNew(group).Add(key);
            }

            var serialized = messagingSerializer.Serialize(value);

            await messagingSubscriptionStore.SubscribeAsync(group, key, serialized, clock.UtcNow, expiresAfter, ct);

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
                localSubscriptions.Remove(key);
            }

            return messagingSubscriptionStore.UnsubscribeAsync(group, key, ct);
        }
    }
}
