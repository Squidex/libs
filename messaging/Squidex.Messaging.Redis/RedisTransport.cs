﻿// ==========================================================================
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

namespace Squidex.Messaging.Redis
{
    public sealed class RedisTransport : ITransport
    {
        private readonly ILogger<RedisTransport> log;
        private readonly RedisTransportOptions options;
        private ISubscriber? subscriber;
        private IDatabase? database;

        public RedisTransport(IOptions<RedisTransportOptions> options,
            ILogger<RedisTransport> log)
        {
            this.options = options.Value;

            this.log = log;
        }

        public async Task InitializeAsync(
            CancellationToken ct)
        {
            var connection = await options.ConnectAsync(new LoggerTextWriter(log));

            database = connection.GetDatabase(options.Database);

            // Is only needed for topics, but it has only minor costs.
            subscriber = connection.GetSubscriber();
        }

        public Task CreateChannelAsync(ChannelName channel, ProducerOptions producerOptions,
            CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public async Task ProduceAsync(ChannelName target, TransportMessage transportMessage,
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
                await subscriber.PublishAsync(GetTopicName(target.Name), json);
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
}
