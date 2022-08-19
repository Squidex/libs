// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Squidex.Messaging.Implementation;

namespace Squidex.Messaging.Mongo
{
    public sealed class MongoTransport : ITransport
    {
        private readonly Dictionary<string, Task<IMongoCollection<MongoMessage>>> collections = new Dictionary<string, Task<IMongoCollection<MongoMessage>>>();
        private readonly MongoTransportOptions options;
        private readonly IMongoDatabase database;
        private readonly ISubscriptionManager subscriptions;
        private readonly IClock clock;
        private readonly ILogger<MongoTransport> log;

        public MongoTransport(IMongoDatabase database, ISubscriptionManager subscriptions,
            IOptions<MongoTransportOptions> options, IClock clock, ILogger<MongoTransport> log)
        {
            this.options = options.Value;
            this.database = database;
            this.subscriptions = subscriptions;
            this.clock = clock;
            this.log = log;
        }

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

        public Task CreateChannelAsync(ChannelName channel, ProducerOptions options,
            CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public async Task ProduceAsync(ChannelName channel, TransportMessage transportMessage,
            CancellationToken ct)
        {
            IReadOnlyList<string> queues;

            if (channel.Type == ChannelType.Queue)
            {
                queues = new List<string> { channel.Name };
            }
            else
            {
                queues = await subscriptions.GetSubscriptionsAsync(channel.Name, ct);
            }

            if (queues.Count == 0)
            {
                return;
            }

            var request = new MongoMessage
            {
                Id = transportMessage.Headers[HeaderNames.Id],
                MessageData = transportMessage.Data,
                MessageHeaders = transportMessage.Headers,
                TimeToLive = GetTimeToLive(transportMessage.Headers),
            };

            foreach (var queueName in queues)
            {
                await ProduceCoreAsync(queueName, request, ct);
            }
        }

        private async Task ProduceCoreAsync(string queueName, MongoMessage request,
            CancellationToken ct)
        {
            var collection = await GetCollectionAsync(queueName);

            await collection.InsertOneAsync(request, null, ct);
        }

        public async Task<IAsyncDisposable> SubscribeAsync(ChannelName channel, string instanceName, MessageTransportCallback callback,
            CancellationToken ct)
        {
            var collectionName = GetCollectioName(channel, instanceName);
            var collectionInstance = await GetCollectionAsync(collectionName);

            return new MongoSubscription(callback, collectionInstance, collectionName, options, clock, log);
        }

        public async Task<IAsyncDisposable?> CreateCleanerAsync(ChannelName channel, string instanceName, TimeSpan timeout, TimeSpan expires,
            CancellationToken ct)
        {
            var collectionName = GetCollectioName(channel, instanceName);
            var collectionInstance = await GetCollectionAsync(collectionName);

            IAsyncDisposable? subscription = null;

            if (channel.Type == ChannelType.Topic)
            {
                subscription = await subscriptions.SubscribeAsync(channel.Name, collectionName, ct);
            }

            IAsyncDisposable result = new MongoTransportCleaner(collectionInstance, collectionName, timeout, expires, options.UpdateInterval, log, clock);

            if (subscription != null)
            {
                result = new AggregateAsyncDisposable(result, subscription);
            }

            return result;
        }

        private static string GetCollectioName(ChannelName channel, string instanceName)
        {
            if (channel.Type == ChannelType.Topic)
            {
                return $"{channel.Name}_{instanceName}";
            }

            return channel.Name;
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
                        new[]
                        {
                            new CreateIndexModel<MongoMessage>(
                                Builders<MongoMessage>.IndexKeys
                                    .Ascending(x => x.TimeHandled)),
                            new CreateIndexModel<MongoMessage>(
                                Builders<MongoMessage>.IndexKeys
                                    .Ascending(x => x.TimeToLive),
                                new CreateIndexOptions
                                {
                                    ExpireAfter = TimeSpan.Zero,
                                })
                        },
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

        private DateTime GetTimeToLive(TransportHeaders headers)
        {
            var time = TimeSpan.FromDays(30);

            if (headers.TryGetTimestamp(HeaderNames.TimeExpires, out var expires))
            {
                time = expires;
            }

            return clock.UtcNow + time;
        }
    }
}
