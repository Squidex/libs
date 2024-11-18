// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Squidex.Hosting;

namespace Squidex.Messaging.Mongo;

public sealed class MongoMessagingDataStore(IMongoDatabase database, IOptions<MongoMessagingDataOptions> options) : IMessagingDataStore, IInitializable
{
    private readonly IMongoCollection<Entity> collection = database.GetCollection<Entity>(options.Value.CollectionName);

    private sealed class Entity
    {
        public string Id { get; set; }

        public string Group { get; set; }

        public string Key { get; set; }

        public string ValueType { get; set; }

        public string ValueFormat { get; set; }

        public byte[] ValueData { get; set; }

        public DateTime Expiration { get; set; }
    }

    public Task InitializeAsync(
        CancellationToken ct)
    {
        return collection.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Entity>(
                    Builders<Entity>.IndexKeys
                        .Ascending(x => x.Group)
                        .Ascending(x => x.Key)),
                new CreateIndexModel<Entity>(
                    Builders<Entity>.IndexKeys
                        .Ascending(x => x.Expiration),
                    new CreateIndexOptions
                    {
                        ExpireAfter = TimeSpan.Zero
                    })
            ], ct);
    }

    public async Task<IReadOnlyList<Entry>> GetEntriesAsync(string group,
        CancellationToken ct)
    {
        var result = new List<Entry>();

        var cursor = await collection.Find(x => x.Group == group).ToCursorAsync(ct);

        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var item in cursor.Current)
            {
                var value = new SerializedObject(item.ValueData, item.ValueType, item.ValueFormat);

                result.Add(new Entry(group, item.Key, value, item.Expiration));
            }
        }

        return result;
    }

    public async Task StoreManyAsync(Entry[] requests,
        CancellationToken ct)
    {
        List<WriteModel<Entity>>? updates = null;

        foreach (var (group, key, value, expiration) in requests)
        {
            updates ??= [];
            updates.Add(new UpdateOneModel<Entity>(
                Builders<Entity>.Filter.Eq(x => x.Id, GetId(group, key)),
                Builders<Entity>.Update
                    .SetOnInsert(x => x.Group, group)
                    .SetOnInsert(x => x.Key, key)
                    .Set(x => x.Expiration, expiration)
                    .Set(x => x.ValueType, value.TypeString)
                    .Set(x => x.ValueFormat, value.Format)
                    .Set(x => x.ValueData, value.Data))
            {
                IsUpsert = true
            });
        }

        if (updates?.Count > 0)
        {
            await collection.BulkWriteAsync(updates, cancellationToken: ct);
        }
    }

    public Task DeleteAsync(string group, string key,
        CancellationToken ct)
    {
        string id = GetId(group, key);

        return collection.DeleteOneAsync(x => x.Id == id, ct);
    }

    private static string GetId(string group, string key)
    {
        return $"{group}/{key}";
    }
}
