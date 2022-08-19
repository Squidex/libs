// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting;

namespace Squidex.Messaging.Implementation
{
    public sealed class DefaultSubscriptionManager : ISubscriptionManager, IBackgroundProcess
    {
        private readonly HashSet<string> localSubscriptions = new HashSet<string>();
        private readonly MessagingOptions options;
        private readonly ISubscriptionStore store;
        private readonly ILogger<DefaultSubscriptionManager> log;
        private readonly IClock clock;
        private SimpleTimer? timer;

        public DefaultSubscriptionManager(ISubscriptionStore store, IOptions<MessagingOptions> options,
            ILogger<DefaultSubscriptionManager> log, IClock clock)
        {
            this.options = options.Value;
            this.store = store;
            this.log = log;
            this.clock = clock;
        }

        public Task<IReadOnlyList<string>> GetSubscriptionsAsync(string topic,
            CancellationToken ct)
        {
            var now = clock.UtcNow;

            return store.GetSubscriptionsAsync(topic, now, ct);
        }

        public Task StartAsync(
            CancellationToken ct)
        {
            if (timer != null)
            {
                return Task.CompletedTask;
            }

            timer = new SimpleTimer(async ct =>
            {
                string[] queues;

                lock (localSubscriptions)
                {
                    queues = localSubscriptions.ToArray();
                }

                if (queues.Length == 0)
                {
                    return;
                }

                await store.UpdateAliveAsync(queues, clock.UtcNow, ct);
            }, options.SubscriptionUpdateInterval, log);

            return Task.CompletedTask;
        }

        public async Task StopAsync(
            CancellationToken ct)
        {
            if (timer == null)
            {
                return;
            }

            await timer.DisposeAsync();

            timer = null;
        }

        public async Task<IAsyncDisposable> SubscribeAsync(string topic, string queue,
            CancellationToken ct)
        {
            lock (localSubscriptions)
            {
                localSubscriptions.Add(queue);
            }

            var now = clock.UtcNow;

            await store.SubscribeAsync(topic, queue, now, options.SubscriptionTimeout, ct);

            return new DelegateAsyncDisposable(() =>
            {
                return new ValueTask(UnsubscribeAsync(topic, queue, default));
            });
        }

        public Task UnsubscribeAsync(string topic, string queue,
            CancellationToken ct)
        {
            lock (localSubscriptions)
            {
                localSubscriptions.Remove(queue);
            }

            return store.UnsubscribeAsync(topic, queue, ct);
        }
    }
}
