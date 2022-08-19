﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Squidex.Assets
{
    public sealed class MongoAssetKeyValueStore<T> : IAssetKeyValueStore<T>
    {
        private readonly UpdateOptions upsert = new UpdateOptions
        {
            IsUpsert = true
        };
        private readonly IMongoCollection<MongoAssetEntity<T>> collection;

        public MongoAssetKeyValueStore(IMongoDatabase database)
        {
            var collectionName = $"AssetKeyValueStore_{typeof(T).Name}";

            collection = database.GetCollection<MongoAssetEntity<T>>(collectionName);
        }

        public Task InitializeAsync(
            CancellationToken ct = default)
        {
            BsonClassMap.RegisterClassMap<T>(options =>
            {
                options.AutoMap();
                options.SetIgnoreExtraElements(true);
            });

            return collection.Indexes.CreateOneAsync(
                new CreateIndexModel<MongoAssetEntity<T>>(
                    Builders<MongoAssetEntity<T>>.IndexKeys.Ascending(x => x.Expires)),
                cancellationToken: ct);
        }

        public Task DeleteAsync(string key,
            CancellationToken ct = default)
        {
            return collection.DeleteOneAsync(x => x.Key == key, ct);
        }

        public async Task<T> GetAsync(string key,
            CancellationToken ct = default)
        {
            var entity = await collection.Find(x => x.Key == key).FirstOrDefaultAsync(ct);

            return entity != null ? entity.Value! : default!;
        }

        public async IAsyncEnumerable<(string Key, T Value)> GetExpiredEntriesAsync(DateTimeOffset now,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var utcNow = now.UtcDateTime;

            var entities = await collection.Find(x => x.Expires < utcNow).ToCursorAsync(ct);

            while (await entities.MoveNextAsync(ct))
            {
                foreach (var entity in entities.Current)
                {
                    yield return (entity.Key, entity.Value);
                }
            }
        }

        public Task SetAsync(string key, T value, DateTimeOffset expires,
            CancellationToken ct = default)
        {
            var utcExpires = expires.UtcDateTime;

            return collection.UpdateOneAsync(x => x.Key == key,
                Builders<MongoAssetEntity<T>>.Update
                    .Set(x => x.Expires, utcExpires)
                    .Set(x => x.Value, value),
                upsert, ct);
        }
    }
}
