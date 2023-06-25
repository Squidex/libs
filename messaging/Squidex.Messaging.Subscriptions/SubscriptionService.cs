// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Internal;
using Squidex.Messaging.Subscriptions.Internal;
using Squidex.Messaging.Subscriptions.Messages;

namespace Squidex.Messaging.Subscriptions;

public sealed class SubscriptionService : ISubscriptionService, IInitializable, IMessageHandler<SubscriptionsMessageBase>
{
    private readonly ConcurrentDictionary<Guid, IUntypedLocalSubscription> localSubscriptions = new ConcurrentDictionary<Guid, IUntypedLocalSubscription>();
    private readonly ConcurrentDictionary<Guid, SubscriptionEntry> clusterSubscriptions = new ConcurrentDictionary<Guid, SubscriptionEntry>();
    private readonly SubscriptionOptions options;
    private readonly string instanceName;
    private readonly IMessageBus messageBus;
    private readonly IMessageEvaluator messageEvaluator;
    private readonly IMessagingSubscriptions messagingSubscriptions;
    private readonly IClock clock;
    private readonly ILogger<SubscriptionService> log;
    private readonly SimpleTimer cleanupTimer;

    public bool HasSubscriptions => clusterSubscriptions.Any();

    public int Order => int.MaxValue;

    public SubscriptionService(
        IInstanceNameProvider instanceName,
        IMessageBus messageBus,
        IMessageEvaluator messageEvaluator,
        IMessagingSubscriptions messagingSubscriptions,
        IClock clock,
        IOptions<SubscriptionOptions> options,
        ILogger<SubscriptionService> log)
    {
        this.instanceName = instanceName.Name;
        this.messageBus = messageBus;
        this.messageEvaluator = messageEvaluator;
        this.messagingSubscriptions = messagingSubscriptions;
        this.options = options.Value;
        this.clock = clock;
        this.log = log;

        cleanupTimer = new SimpleTimer(CleanupAsync, options.Value.SubscriptionUpdateTime, log);
    }

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        var subscriptions = await messagingSubscriptions.GetSubscriptionsAsync<ISubscription>(options.GroupName, ct);

        foreach (var (key, subscription) in subscriptions)
        {
            SubscribeAsClusterSubscription(Guid.Parse(key), subscription);
        }
    }

    public Task ReleaseAsync(
        CancellationToken ct)
    {
        return cleanupTimer.DisposeAsync().AsTask();
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
                SourceId = instanceName
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
                    UnsubscribeAsClusterSubscription(entry.SubscriptionId);
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
        if (message.SourceId == instanceName)
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

                SubscribeAsClusterSubscription(subscribe.SubscriptionId, subscribe.GetUntypedSubscription());
                break;

            case UnsubscribeMessage unsubscribe:
                log.LogDebug("Received unsubscribe from {sender}.", unsubscribe.SourceId);

                UnsubscribeAsClusterSubscription(unsubscribe.SubscriptionId);
                break;

            case SubscriptionsAliveMessage alive:
                log.LogDebug("Received alive message from {sender}.", alive.SourceId);

                UpdateClusterSubscriptions(alive.SubscriptionIds);
                break;
        }

        return Task.CompletedTask;
    }

    public IObservable<T> Subscribe<T>()
    {
        return new LocalSubscription<T>(this, new Subscription<T>());
    }

    public IObservable<T> Subscribe<T>(ISubscription subscription)
    {
        Guard.NotNull(subscription, nameof(subscription));

        return new LocalSubscription<T>(this, subscription);
    }

    public Task PublishAsync(object message)
    {
        Guard.NotNull(message, nameof(message));

        if (message is IPayloadWrapper wrapper)
        {
            return PublishWrapperAsync(wrapper);
        }
        else
        {
            return PublishCoreAsync(message);
        }
    }

    private async Task PublishCoreAsync(object message)
    {
        List<Guid>? remoteSubscriptionIds = null;

        foreach (var id in await messageEvaluator.GetSubscriptionsAsync(message))
        {
            if (!options.SendMessagesToSelf && localSubscriptions.TryGetValue(id, out var localSubscription))
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

        await PublishCoreAsync(remoteSubscriptionIds, message);
    }

    private async Task PublishWrapperAsync(IPayloadWrapper wrapper)
    {
        List<Guid>? remoteSubscriptionIds = null;

        var message = (object)null!;

        foreach (var id in await messageEvaluator.GetSubscriptionsAsync(wrapper.Message))
        {
            if (!options.SendMessagesToSelf && localSubscriptions.TryGetValue(id, out var localSubscription))
            {
                message ??= await wrapper.CreatePayloadAsync();

                if (message != null)
                {
                    localSubscription.OnNext(message);
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

        message ??= await wrapper.CreatePayloadAsync();

        if (message == null)
        {
            return;
        }

        await PublishCoreAsync(remoteSubscriptionIds, message!);
    }

    private Task PublishCoreAsync(List<Guid> remoteSubscriptionIds, object message)
    {
        var sourceId = options.SendMessagesToSelf ? null : instanceName;

        return messageBus.PublishAsync(
            MessageFactories.Payload(remoteSubscriptionIds, message, sourceId));
    }

    internal void SubscribeCore(Guid id, IUntypedLocalSubscription localSubscription, ISubscription subscription)
    {
        var expires = options.SubscriptionExpirationTime;

        // Also store the subscription in the database to have them available for the next start.
        messagingSubscriptions.SubscribeAsync(options.GroupName, id.ToString(), subscription, expires).Forget();

        messageBus.PublishAsync(
            MessageFactories.Subscribe(id, subscription, instanceName)).Forget();

        localSubscriptions[id] = localSubscription;

        SubscribeAsClusterSubscription(id, subscription);
    }

    internal void UnsubscribeCore(Guid id)
    {
        // Also remove the subscription from the store, so it does not get restored.
        messagingSubscriptions.UnsubscribeAsync(options.GroupName, id.ToString()).Forget();

        messageBus.PublishAsync(
            MessageFactories.Unsubscribe(id, instanceName)).Forget();

        localSubscriptions.TryRemove(id, out _);

        UnsubscribeAsClusterSubscription(id);
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

    private void SubscribeAsClusterSubscription(Guid id, ISubscription subscription)
    {
        if (clusterSubscriptions.TryAdd(id, new SubscriptionEntry(id, subscription, GetNextExpiration())))
        {
            // The evaluator maintains a custom list to optimize the data structure for faster evaluation.
            messageEvaluator.SubscriptionAdded(id, subscription);
        }
    }

    private void UnsubscribeAsClusterSubscription(Guid id)
    {
        if (clusterSubscriptions.TryRemove(id, out var entry))
        {
            // The evaluator might contain a copy of the subscription, therefore remove as well.
            messageEvaluator.SubscriptionRemoved(id, entry.Subscription);
        }
    }

    private DateTime GetNextExpiration()
    {
        return clock.UtcNow + options.SubscriptionExpirationTime;
    }
}
