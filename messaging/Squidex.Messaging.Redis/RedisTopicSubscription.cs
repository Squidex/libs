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
    private readonly ChannelMessageQueue messageQueue;

    public RedisTopicSubscription(string topicName, ISubscriber subscriber, MessageTransportCallback callback,
        ILogger log)
    {
        var channel = new RedisChannel(topicName, RedisChannel.PatternMode.Literal);

        // Subscribe with a message queue instead of a handler. A handler would be invoked on the
        // connection thread and blocking that thread for the duration of the message handler would
        // stall the multiplexer for all other subscriptions.
        messageQueue = subscriber.Subscribe(channel);

        // The messages are still processed sequentially, but on a dedicated task.
        messageQueue.OnMessage(async message =>
        {
            try
            {
                var deserialized = JsonSerializer.Deserialize<TransportMessage>(message.Message.ToString())!;

                await callback(new TransportResult(deserialized, null), this, default);
            }
            catch (Exception ex)
            {
                LogMessages.FailedToDeserializeMessage(log, ex);
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        await messageQueue.UnsubscribeAsync();
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
