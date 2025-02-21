﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Internal;

namespace Squidex.Messaging.Mongo;

public sealed class MongoTransport(
    IMongoDatabase database,
    IMessagingDataProvider messagingDataProvider,
    IOptions<MongoTransportOptions> options,
    TimeProvider timeProvider,
    ILogger<MongoTransport> log)
    : IMessagingTransport
{
    private readonly Dictionary<string, Task<IMongoCollection<MongoMessage>>> collections = [];
    private readonly MongoTransportOptions options = options.Value;

    public Task InitializeAsync(
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(
       CancellationToken ct)
    {
        collections.Clear();

        return Task.CompletedTask;
    }

    public async Task<IAsyncDisposable?> CreateChannelAsync(ChannelName channel, string instanceName, bool consume, ProducerOptions options,
        CancellationToken ct)
    {
        if (!consume)
        {
            return null;
        }

        var channelName = channel.Name;
        var channelCollection = await GetCollectionAsync(channelName);

        IAsyncDisposable? subscription = null;
        if (channel.Type == ChannelType.Topic)
        {
            var value = new MongoSubscriptionValue
            {
                InstanceName = instanceName,
            };

            subscription = await messagingDataProvider.StoreAsync(channelName, instanceName, value, this.options.SubscriptionExpiration, ct);
        }

        IAsyncDisposable result = new MongoTransportCleaner(
            channelCollection,
            channelName,
            options.Timeout,
            options.Expires,
            this.options.UpdateInterval,
            log,
            timeProvider);

        if (subscription != null)
        {
            result = new AggregateAsyncDisposable(result, subscription);
        }

        return result;
    }

    public async Task ProduceAsync(ChannelName channel, string instanceName, TransportMessage transportMessage,
        CancellationToken ct)
    {
        IReadOnlyList<string> queues;

        var channelName = channel.Name;
        if (channel.Type == ChannelType.Queue)
        {
            queues = [channelName];
        }
        else
        {
            var subscribed = await messagingDataProvider.GetEntriesAsync<MongoSubscriptionValue>(channel.Name, ct);

            queues = subscribed.Select(x => x.Value.InstanceName).ToList();
        }

        if (queues.Count == 0)
        {
            return;
        }

        // Only use one collection per topic for all queues, because it is easier to deal with outdated subscriptions.
        var channelCollection = await GetCollectionAsync(channelName);

        foreach (var queueName in queues)
        {
            var mongoMessage = new MongoMessage
            {
                Id = $"{transportMessage.Headers[HeaderNames.Id]}_{queueName}",
                QueueName = queueName,
                MessageData = transportMessage.Data,
                MessageHeaders = transportMessage.Headers,
                TimeToLive = transportMessage.Headers.GetTimeToLive(timeProvider),
            };

            await channelCollection.InsertOneAsync(mongoMessage, null, ct);
        }
    }

    public async Task<IAsyncDisposable> SubscribeAsync(ChannelName channel, string instanceName, MessageTransportCallback callback,
        CancellationToken ct)
    {
        var queueFilter = channel.Type == ChannelType.Topic ? instanceName : null;

        var channelName = channel.Name;
        var channelCollection = await GetCollectionAsync(channelName);

        return new MongoSubscription(
            callback,
            channelCollection,
            channelName,
            queueFilter,
            options,
            timeProvider,
            log);
    }

    private async Task<IMongoCollection<MongoMessage>> GetCollectionAsync(string name)
    {
        Task<IMongoCollection<MongoMessage>> collectionTask;

        lock (collections)
        {
            async Task<IMongoCollection<MongoMessage>> CreateCollectionAsync()
            {
                var collection = database.GetCollection<MongoMessage>($"{options.CollectionName}_{name}");

                await collection.Indexes.CreateManyAsync(
                    [
                        new CreateIndexModel<MongoMessage>(
                            Builders<MongoMessage>.IndexKeys
                                .Ascending(x => x.TimeHandled)
                                .Ascending(x => x.QueueName)),
                        new CreateIndexModel<MongoMessage>(
                            Builders<MongoMessage>.IndexKeys
                                .Ascending(x => x.TimeToLive),
                            new CreateIndexOptions
                            {
                                ExpireAfter = TimeSpan.Zero,
                            }),
                    ],
                    default);

                return collection;
            }

            if (!collections.TryGetValue(name, out collectionTask!))
            {
                collectionTask = CreateCollectionAsync();
                collections[name] = collectionTask;
            }
        }

        return await collectionTask;
    }
}
