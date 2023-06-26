// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Squidex.Messaging.Redis;

internal sealed class RedisTopicSubscription : IAsyncDisposable, IMessageAck
{
    private readonly Action unsubscribe;

    public RedisTopicSubscription(string topicName, ISubscriber subscriber, MessageTransportCallback callback,
        ILogger log)
    {
        var handler = new Action<RedisChannel, RedisValue>((_, message) =>
        {
            try
            {
                var deserialized = JsonSerializer.Deserialize<TransportMessage>(message.ToString())!;

                callback(new TransportResult(deserialized, null), this, default).Wait();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to deserialize message.");
            }
        });

        var channel = new RedisChannel(topicName, RedisChannel.PatternMode.Literal);

        subscriber.Subscribe(channel, handler);

        unsubscribe = () =>
        {
            subscriber.Unsubscribe(channel, handler);
        };
    }

    public ValueTask DisposeAsync()
    {
        unsubscribe();

        return default;
    }

    Task IMessageAck.OnErrorAsync(TransportResult result,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    Task IMessageAck.OnSuccessAsync(TransportResult result,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
