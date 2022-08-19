// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Squidex.Hosting;
using Squidex.Messaging.Implementation;

namespace Squidex.Messaging.Mongo
{
    public sealed class MongoSubscriptionStore : ISubscriptionStore, IInitializable
    {
        private readonly IMongoCollection<Entity> collection;

        private sealed class Entity
        {
            public string Id { get; set; }

            public string Queue { get; set; }

            public string Topic { get; set; }

            public TimeSpan ExpiresAfter { get; set; }

            public DateTime LastActivity { get; set; }
        }

        public MongoSubscriptionStore(IMongoDatabase database, IOptions<MongoSubscriptionStoreOptions> options)
        {
            collection = database.GetCollection<Entity>(options.Value.CollectionName);
        }

        public Task InitializeAsync(
            CancellationToken ct)
        {
            return collection.Indexes.CreateManyAsync(
                new[]
                {
                    new CreateIndexModel<Entity>(
                        Builders<Entity>.IndexKeys.Ascending(x => x.Topic)),
                    new CreateIndexModel<Entity>(
                        Builders<Entity>.IndexKeys.Ascending(x => x.Queue))
                }, ct);
        }

        public async Task<IReadOnlyList<string>> GetSubscriptionsAsync(string topic, DateTime now,
            CancellationToken ct)
        {
            var result = new List<string>();

            var cursor = await collection.Find(x => x.Topic == topic).ToCursorAsync(ct);

            while (await cursor.MoveNextAsync(ct))
            {
                foreach (var item in cursor.Current)
                {
                    if (!IsExpired(item, now))
                    {
                        result.Add(item.Queue);
                    }
                }
            }

            return result;
        }

        public Task SubscribeAsync(string topic, string queue, DateTime now, TimeSpan expiresAfter,
            CancellationToken ct)
        {
            string id = GetId(topic, queue);

            return collection.UpdateOneAsync(x => x.Id == id,
                Builders<Entity>.Update
                    .SetOnInsert(x => x.Topic, topic)
                    .SetOnInsert(x => x.Queue, queue)
                    .Set(x => x.ExpiresAfter, expiresAfter)
                    .Set(x => x.LastActivity, now),
                new UpdateOptions
                {
                    IsUpsert = true
                },
                ct);
        }

        public Task UnsubscribeAsync(string topic, string queue,
            CancellationToken ct)
        {
            string id = GetId(topic, queue);

            return collection.DeleteOneAsync(x => x.Id == id, ct);
        }

        public Task UpdateAliveAsync(string[] queues, DateTime now,
            CancellationToken ct)
        {
            return collection.UpdateOneAsync(x => queues.Contains(x.Queue),
                Builders<Entity>.Update
                    .Set(x => x.LastActivity, now),
                null, ct);
        }

        public async Task CleanupAsync(DateTime now,
            CancellationToken ct)
        {
            List<DeleteOneModel<Entity>>? deletions = null;

            var cursor = await collection.Find(new BsonDocument()).ToCursorAsync(ct);

            while (await cursor.MoveNextAsync(ct))
            {
                foreach (var item in cursor.Current)
                {
                    if (IsExpired(item, now))
                    {
                        deletions ??= new List<DeleteOneModel<Entity>>();
                        deletions.Add(new DeleteOneModel<Entity>(Builders<Entity>.Filter.Eq(x => x.Id, item.Id)));
                    }
                }
            }

            if (deletions?.Count > 0)
            {
                await collection.BulkWriteAsync(deletions, cancellationToken: ct);
            }
        }

        private static bool IsExpired(Entity item, DateTime now)
        {
            return item.ExpiresAfter > TimeSpan.Zero && item.LastActivity + item.ExpiresAfter < now;
        }

        private static string GetId(string topic, string queue)
        {
            return $"{topic}/{queue}";
        }
    }
}
