// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Messaging.Internal;
using StackExchange.Redis;

namespace Squidex.Messaging.Redis;

public sealed class RedisTransport(
    IOptions<RedisTransportOptions> options,
    ILogger<RedisTransport> log)
    : IMessagingTransport
{
    private readonly RedisTransportOptions options = options.Value;
    private ISubscriber? subscriber;
    private IDatabase? database;

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        var connection = await options.ConnectAsync(new LoggerTextWriter(log));

        database = connection.GetDatabase(options.Database);

        // Is only needed for topics, but it has only minor costs.
        subscriber = connection.GetSubscriber();
    }

    public Task<IAsyncDisposable?> CreateChannelAsync(ChannelName channel, string instanceName, bool consume, ProducerOptions producerOptions,
        CancellationToken ct)
    {
        return Task.FromResult<IAsyncDisposable?>(null);
    }

    public async Task ProduceAsync(ChannelName target, string instanceName, TransportMessage transportMessage,
        CancellationToken ct)
    {
        if (subscriber == null || database == null)
        {
            ThrowHelper.InvalidOperationException("Transport not initialized yet.");
            return;
        }

        var json = JsonSerializer.Serialize(transportMessage);

        if (target.Type == ChannelType.Topic)
        {
            var channel = new RedisChannel(GetTopicName(target.Name), RedisChannel.PatternMode.Literal);

            await subscriber.PublishAsync(channel, json);
        }
        else
        {
            await database.ListLeftPushAsync(GetQueueName(target.Name), json);
        }
    }

    public Task<IAsyncDisposable> SubscribeAsync(ChannelName channel, string instanceName, MessageTransportCallback callback,
        CancellationToken ct)
    {
        return Task.FromResult(SubscribeCore(channel, callback));
    }

    private IAsyncDisposable SubscribeCore(ChannelName channel, MessageTransportCallback callback)
    {
        if (subscriber == null || database == null)
        {
            ThrowHelper.InvalidOperationException("Transport not initialized yet.");
            return null!;
        }

        if (channel.Type == ChannelType.Topic)
        {
            return new RedisTopicSubscription(GetTopicName(channel.Name), subscriber, callback, log);
        }
        else
        {
            return new RedisQueueSubscription(GetQueueName(channel.Name), database, callback, options.PollingInterval, log);
        }
    }

    private string GetTopicName(string name)
    {
        return $"{options.TopicPrefix}_{name}";
    }

    private string GetQueueName(string name)
    {
        return $"{options.QueuePrefix}_{name}";
    }
}
