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

namespace Squidex.Messaging.Mongo
{
    public sealed class MongoSubscriptionStore : IMessagingSubscriptionStore, IInitializable
    {
        private readonly IMongoCollection<Entity> collection;

        private sealed class Entity
        {
            public string Id { get; set; }

            public string Group { get; set; }

            public string Key { get; set; }

            public string ValueType { get; set; }

            public string ValueFormat { get; set; }

            public byte[] ValueData { get; set; }

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
                        Builders<Entity>.IndexKeys
                            .Ascending(x => x.Group)
                            .Ascending(x => x.Key))
                }, ct);
        }

        public async Task<IReadOnlyList<(string Key, SerializedObject Value)>> GetSubscriptionsAsync(string group, DateTime now,
            CancellationToken ct)
        {
            var result = new List<(string, SerializedObject)>();

            var cursor = await collection.Find(x => x.Group == group).ToCursorAsync(ct);

            while (await cursor.MoveNextAsync(ct))
            {
                foreach (var item in cursor.Current)
                {
                    if (!IsExpired(item, now))
                    {
                        var value = new SerializedObject(item.ValueData, item.ValueType, item.ValueFormat);

                        result.Add((item.Key, value));
                    }
                }
            }

            return result;
        }

        public Task SubscribeAsync(string group, string key, SerializedObject value, DateTime now, TimeSpan expiresAfter,
            CancellationToken ct)
        {
            string id = GetId(group, key);

            return collection.UpdateOneAsync(x => x.Id == id,
                Builders<Entity>.Update
                    .SetOnInsert(x => x.Group, group)
                    .SetOnInsert(x => x.Key, key)
                    .Set(x => x.ExpiresAfter, expiresAfter)
                    .Set(x => x.ValueType, value.TypeString)
                    .Set(x => x.ValueFormat, value.Format)
                    .Set(x => x.ValueData, value.Data)
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

        public Task UpdateAliveAsync(string group, string[] queues, DateTime now,
            CancellationToken ct)
        {
            var ids = queues.Select(x => GetId(group, x)).ToList();

            return collection.UpdateManyAsync(x => ids.Contains(x.Id),
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
