// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using System.Reflection;

namespace Squidex.Messaging.Subscriptions.Messages;

internal static class MessageFactories
{
    private static readonly ConcurrentDictionary<Type, Func<Guid, ISubscription, SubscribeMessageBase>> SubscribeFactories = new ();
    private static readonly ConcurrentDictionary<Type, Func<List<Guid>, object, PayloadMessageBase>> PayloadFactories = new ();

    public static SubscriptionsMessageBase Subscribe(Guid id, ISubscription subscription, string? sourceId)
    {
        var factory = SubscribeFactories.GetOrAdd(subscription.GetType(), type =>
        {
            var method = typeof(MessageFactories).GetMethod(nameof(BuildSubscribeFactory), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);

            return (Func<Guid, ISubscription, SubscribeMessageBase>)method.Invoke(null, Array.Empty<object?>())!;
        });

        var result = factory(id, subscription);

        return Enrich(result, sourceId);
    }

    public static SubscriptionsMessageBase Payload(List<Guid> ids, object message, string? sourceId)
    {
        var factory = PayloadFactories.GetOrAdd(message.GetType(), type =>
        {
            var method = typeof(MessageFactories).GetMethod(nameof(BuildPayloadFactory), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);

            return (Func<List<Guid>, object, PayloadMessageBase>)method.Invoke(null, Array.Empty<object?>())!;
        });

        var result = factory(ids, message);

        return Enrich(result, sourceId);
    }

    public static SubscriptionsMessageBase Unsubscribe(Guid id, string? sourceId)
    {
        var result = new UnsubscribeMessage
        {
            SubscriptionId = id
        };

        return Enrich(result, sourceId);
    }

    public static SubscriptionsMessageBase Alive(Guid id, string? sourceId)
    {
        var result = new UnsubscribeMessage
        {
            SubscriptionId = id
        };

        return Enrich(result, sourceId);
    }

    private static SubscriptionsMessageBase Enrich(SubscriptionsMessageBase result, string? sourceId)
    {
        // Ensure that we do not publish to the the current instance.
        result.SourceId = sourceId;

        return result;
    }

    private static Func<List<Guid>, object, PayloadMessageBase> BuildPayloadFactory<T>() where T : notnull
    {
        return (ids, message) =>
        {
            return new PayloadMessage<T>
            {
                SubscriptionMessage = (T)message,
                SubscriptionIds = ids
            };
        };
    }

    private static Func<Guid, object, SubscribeMessageBase> BuildSubscribeFactory<T>() where T : ISubscription
    {
        return (id, subscription) =>
        {
            return new SubscribeMessage<T>
            {
                Subscription = (T)subscription,
                SubscriptionId = id
            };
        };
    }
}
