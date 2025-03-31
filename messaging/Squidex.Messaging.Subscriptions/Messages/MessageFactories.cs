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
    private static readonly ConcurrentDictionary<Type, Func<List<string>, string?, object, PayloadMessageBase>> PayloadFactories = [];

    private static readonly MethodInfo BuildPayloadFactoryMethod =
        typeof(MessageFactories).GetMethod(nameof(BuildPayloadFactory), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static PayloadMessageBase Payload<T>(List<string> ids, T message, string? sourceId) where T : notnull
    {
        var factory = PayloadFactories.GetOrAdd(message.GetType(), type =>
        {
            var method = BuildPayloadFactoryMethod.MakeGenericMethod(type);

            return (Func<List<string>, string?, object, PayloadMessageBase>)method.Invoke(null, [])!;
        });

        return factory(ids, sourceId, message);
    }

    private static Func<List<string>, string?, object, PayloadMessageBase> BuildPayloadFactory<T>() where T : notnull
    {
        return (ids, sourceId, message) =>
        {
            return new PayloadMessage<T>
            {
                SubscriptionMessage = (T)message,
                SubscriptionIds = ids,
                SourceId = sourceId,
            };
        };
    }
}
