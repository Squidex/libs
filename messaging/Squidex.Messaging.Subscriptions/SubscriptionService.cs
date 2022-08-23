// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Internal;
using Squidex.Messaging.Subscriptions.Internal;
using Squidex.Messaging.Subscriptions.Messages;

namespace Squidex.Messaging.Subscriptions
{
    public sealed class SubscriptionService : ISubscriptionService, IAsyncDisposable, IMessageHandler<SubscriptionsMessageBase>
    {
        private readonly ConcurrentDictionary<Guid, IUntypedLocalSubscription> localSubscriptions = new ConcurrentDictionary<Guid, IUntypedLocalSubscription>();
        private readonly ConcurrentDictionary<Guid, SubscriptionEntry> clusterSubscriptions = new ConcurrentDictionary<Guid, SubscriptionEntry>();
        private readonly SubscriptionOptions options;
        private readonly IInstanceNameProvider instanceNameProvider;
        private readonly IMessageBus messageBus;
        private readonly IMessageEvaluator messageEvaluator;
        private readonly IClock clock;
        private readonly ILogger<SubscriptionService> log;
        private readonly SimpleTimer cleanupTimer;

        public bool HasSubscriptions => clusterSubscriptions.Any();

        public SubscriptionService(
            IInstanceNameProvider instanceNameProvider,
            IMessageBus messageBus,
            IMessageEvaluator messageEvaluator,
            IClock clock,
            IOptions<SubscriptionOptions> options,
            ILogger<SubscriptionService> log)
        {
            this.options = options.Value;
            this.instanceNameProvider = instanceNameProvider;
            this.messageEvaluator = messageEvaluator;
            this.messageBus = messageBus;
            this.clock = clock;
            this.log = log;

            cleanupTimer = new SimpleTimer(CleanupAsync, options.Value.SubscriptionUpdateTime, log);
        }

        public ValueTask DisposeAsync()
        {
            return cleanupTimer.DisposeAsync();
        }

        public async Task CleanupAsync(
            CancellationToken ct)
        {
            if (!localSubscriptions.IsEmpty)
            {
                // Only publish a message if there is at least one subscription.
                await messageBus.PublishAsync(new SubscriptionsAliveMessage
                {
                    SubscriptionIds = localSubscriptions.Keys.ToList(),

                    // Ensure that we do not update the current instance.
                    SourceId = instanceNameProvider.Name
                }, ct: ct);
            }

            if (!clusterSubscriptions.IsEmpty)
            {
                var now = clock.UtcNow;

                var numExpiredSubscriptions = 0;

                foreach (var entry in clusterSubscriptions.Values.ToList())
                {
                    if (entry.ExpiresUtc < now)
                    {
                        RemoveClusterSubscription(entry.SubscriptionId);
                        numExpiredSubscriptions++;
                    }
                }

                if (numExpiredSubscriptions > 0)
                {
                    log.LogInformation("Removed {numExpiredSubscriptions} expired subscriptions.", numExpiredSubscriptions);
                }
            }
        }

        public Task HandleAsync(SubscriptionsMessageBase message,
            CancellationToken ct)
        {
            if (message.SourceId == instanceNameProvider.Name)
            {
                return Task.CompletedTask;
            }

            switch (message)
            {
                case PayloadMessageBase payload:
                    log.LogDebug("Received payload of type {type} from {sender}", payload.GetUntypedPayload()?.GetType(), payload.SourceId);

                    foreach (var subscriptionId in payload.SubscriptionIds)
                    {
                        if (localSubscriptions.TryGetValue(subscriptionId, out var localSubscription))
                        {
                            localSubscription.OnNext(payload.GetUntypedPayload());
                        }
                    }

                    break;

                case SubscribeMessageBase subscribe:
                    log.LogDebug("Received subscription from {sender}.", subscribe.SourceId);

                    AddClusterSubscription(subscribe.SubscriptionId, subscribe.GetUntypedSubscription());
                    break;

                case UnsubscribeMessage unsubscribe:
                    log.LogDebug("Received unsubscribe from {sender}.", unsubscribe.SourceId);

                    RemoveClusterSubscription(unsubscribe.SubscriptionId);
                    break;

                case SubscriptionsAliveMessage alive:
                    log.LogDebug("Received alive message from {sender}.", alive.SourceId);

                    UpdateClusterSubscriptions(alive.SubscriptionIds);
                    break;
            }

            return Task.CompletedTask;
        }

        public ILocalSubscription<T> Subscribe<T, TSubscription>(TSubscription subscription) where TSubscription : ISubscription, new()
        {
            return new LocalSubscription<T, TSubscription>(this, subscription);
        }

        public ILocalSubscription<T> Subscribe<T>()
        {
            return new LocalSubscription<T, Subscription<T>>(this, new Subscription<T>());
        }

        public async Task PublishAsync<T>(T message) where T : notnull
        {
            List<Guid>? remoteSubscriptionIds = null;

            foreach (var id in await messageEvaluator.GetSubscriptionsAsync(message))
            {
                if (localSubscriptions.TryGetValue(id, out var localSubscription))
                {
                    localSubscription.OnNext(message);
                }
                else
                {
                    remoteSubscriptionIds ??= new List<Guid>();
                    remoteSubscriptionIds.Add(id);
                }
            }

            if (remoteSubscriptionIds == null)
            {
                return;
            }

            await PublichCoreasync(remoteSubscriptionIds, message);
        }

        public async Task PublishWrapperAsync<T>(IPayloadWrapper<T> wrapper) where T : notnull
        {
            List<Guid>? remoteSubscriptionIds = null;

            var messageValue = default(T);
            var messageCreated = false;

            foreach (var id in await messageEvaluator.GetSubscriptionsAsync(wrapper))
            {
                if (localSubscriptions.TryGetValue(id, out var localSubscription))
                {
                    if (!messageCreated)
                    {
                        messageCreated = true;
                        messageValue = await wrapper.CreatePayloadAsync();
                    }

                    if (messageValue is not null)
                    {
                        localSubscription.OnNext(messageValue);
                    }
                }
                else
                {
                    remoteSubscriptionIds ??= new List<Guid>();
                    remoteSubscriptionIds.Add(id);
                }
            }

            if (remoteSubscriptionIds == null)
            {
                return;
            }

            if (!messageCreated)
            {
                messageValue = await wrapper.CreatePayloadAsync();
            }

            if (messageValue is null)
            {
                return;
            }

            await PublichCoreasync(remoteSubscriptionIds, messageValue!);
        }

        private async Task PublichCoreasync<T>(List<Guid> remoteSubscriptionIds, T message) where T : notnull
        {
            await messageBus.PublishAsync(new PayloadMessage<T>
            {
                Payload = message!,

                // Publish multiple subscription IDs together.
                SubscriptionIds = remoteSubscriptionIds,

                // Ensure that we do not publish to the the current instance.
                SourceId = instanceNameProvider.Name
            });
        }

        internal void SubscribeCore<TSubscription>(Guid id, IUntypedLocalSubscription localSubscription, TSubscription subscription) where TSubscription : ISubscription
        {
            localSubscriptions[id] = localSubscription;

            messageBus.PublishAsync(new SubscribeMessage<TSubscription>
            {
                SubscriptionId = id,
                Subscription = subscription,

                // Ensure that we do not publish to the the current instance.
                SourceId = instanceNameProvider.Name
            }).Forget();

            AddClusterSubscription(id, subscription);
        }

        internal void UnsubscribeCore(Guid id)
        {
            localSubscriptions.TryRemove(id, out _);

            messageBus.PublishAsync(new UnsubscribeMessage
            {
                SubscriptionId = id,

                // Ensure that we do not publish to the the current instance.
                SourceId = instanceNameProvider.Name
            }).Forget();

            RemoveClusterSubscription(id);
        }

        private void UpdateClusterSubscriptions(List<Guid> subscriptionIds)
        {
            var nextExpiration = GetNextExpiration();

            foreach (var id in subscriptionIds)
            {
                if (clusterSubscriptions.TryGetValue(id, out var entry))
                {
                    entry.UpdateExpiration(nextExpiration);
                }
            }
        }

        private void AddClusterSubscription(Guid id, ISubscription subscription)
        {
            clusterSubscriptions[id] = new SubscriptionEntry(id, subscription, GetNextExpiration());

            // The evaluator maintains a custom list to optimize the data structure for faster evaluation.
            messageEvaluator.SubscriptionAdded(id, subscription);
        }

        private void RemoveClusterSubscription(Guid id)
        {
            if (!clusterSubscriptions.TryRemove(id, out var entry))
            {
                return;
            }

            // The evaluator maintains a custom list to optimize the data structure for faster evaluation.
            messageEvaluator.SubscriptionRemoved(id, entry.Subscription);
        }

        private DateTime GetNextExpiration()
        {
            return clock.UtcNow + options.SubscriptionExpirationTime;
        }
    }
}
