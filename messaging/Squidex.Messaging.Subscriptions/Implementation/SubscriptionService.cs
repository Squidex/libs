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
using Squidex.Text;

namespace Squidex.Messaging.Subscriptions.Implementation;

public sealed class SubscriptionService : ISubscriptionService, IMessageHandler<PayloadMessageBase>
{
    private readonly ConcurrentDictionary<string, IUntypedLocalSubscription> localSubscriptions = [];
    private readonly SubscriptionOptions options;
    private readonly string instanceName;
    private readonly IMessageBus messageBus;
    private readonly IMessagingDataProvider messagingDataProvider;
    private readonly ILogger<SubscriptionService> log;

    public SubscriptionService(
        IInstanceNameProvider instanceName,
        IMessageBus messageBus,
        IMessagingDataProvider messagingDataProvider,
        IOptions<SubscriptionOptions> options,
        ILogger<SubscriptionService> log)
    {
        this.instanceName = instanceName.Name;
        this.messageBus = messageBus;
        this.messagingDataProvider = messagingDataProvider;
        this.options = options.Value;
        this.log = log;
    }

    public async Task<bool> HasSubscriptionsAsync<T>(string key,
        CancellationToken ct = default) where T : notnull
    {
        Guard.NotNullOrEmpty(key, nameof(key));

        // This is usually cached, so it is relatively fast.
        var subscriptions = await messagingDataProvider.GetEntriesAsync<ISubscription<T>>(GroupName<T>(key), ct);

        return subscriptions.Count > 0;
    }

    public Task HandleAsync(PayloadMessageBase message,
        CancellationToken ct)
    {
        if (message.SourceId == instanceName)
        {
            return Task.CompletedTask;
        }

        log.LogDebug("Received payload of type {type} from {sender}", message.GetUntypedPayload()?.GetType(), message.SourceId);

        foreach (var subscriptionId in message.SubscriptionIds)
        {
            if (localSubscriptions.TryGetValue(subscriptionId, out var localSubscription))
            {
                localSubscription.OnNext(message.GetUntypedPayload());
            }
        }

        return Task.CompletedTask;
    }

    public Task<IObservable<T>> SubscribeAsync<T>(string key,
        CancellationToken ct = default) where T : notnull
    {
        Guard.NotNullOrEmpty(key, nameof(key));

        return SubscribeAsync(key, new Subscription<T>(), ct);
    }

    public async Task<IObservable<T>> SubscribeAsync<T>(string key, ISubscription<T> subscription,
        CancellationToken ct = default) where T : notnull
    {
        Guard.NotNullOrEmpty(key, nameof(key));
        Guard.NotNull(subscription, nameof(subscription));

        var local = new LocalSubscription<T>(this, key);

        await SubscribeCore(local.Id, key, local, subscription);

        return local;
    }

    public Task PublishAsync<T>(string key, T message,
        CancellationToken ct = default) where T : notnull
    {
        Guard.NotNullOrEmpty(key, nameof(key));
        Guard.NotNull(message, nameof(message));

        return PublishWrapperAsync(key, new PayloadWrapper<T>(message), ct);
    }

    public async Task PublishWrapperAsync<T>(string key, IPayloadWrapper<T> wrapper,
        CancellationToken ct = default) where T : notnull
    {
        Guard.NotNullOrEmpty(key, nameof(key));
        Guard.NotNull(wrapper, nameof(wrapper));

        List<string>? remoteSubscriptionIds = null;

        // This is usually cached, so it is relatively fast.
        var subscriptions = await messagingDataProvider.GetEntriesAsync<ISubscription<T>>(GroupName<T>(key), ct);

        // Ensure that we only create the payload on demand.
        var payload = (object)null!;

        // Every node has a copy of all subscriptions, therefore we check if there is a subscription somewhere before we send the message.
        foreach (var (id, subscription) in subscriptions)
        {
            if (!await subscription.ShouldHandle(wrapper.Message))
            {
                continue;
            }

            if (localSubscriptions.TryGetValue(id, out var localSubscription))
            {
                // Ensure that we only create the payload on demand.
                payload ??= await wrapper.CreatePayloadAsync();

                localSubscription.OnNext(payload);
            }
            else
            {
                remoteSubscriptionIds ??= [];
                remoteSubscriptionIds.Add(id);
            }
        }

        if (remoteSubscriptionIds == null)
        {
            return;
        }

        // Ensure that we only create the payload on demand.
        payload ??= await wrapper.CreatePayloadAsync();

        await messageBus.PublishAsync(MessageFactories.Payload(remoteSubscriptionIds, payload, instanceName), ct: ct);
    }

    internal async Task SubscribeCore<T>(string id, string key, IUntypedLocalSubscription local, ISubscription<T> subscription)
    {
        localSubscriptions[id] = local;

        // Saves the subscription in the store.
        await messagingDataProvider.StoreAsync(GroupName<T>(key), id, subscription, options.ExpirationTime);
    }

    internal async Task UnsubscribeAsync<T>(string id, string key)
    {
        localSubscriptions.TryRemove(id, out _);

        // Also remove the subscription from the store, so it does not get restored.
        await messagingDataProvider.DeleteAsync(GroupName<T>(key), id);
    }

    private string GroupName<T>(string key)
    {
        return $"{options.GroupName}_{typeof(T).Name.Slugify()}";
    }
}
