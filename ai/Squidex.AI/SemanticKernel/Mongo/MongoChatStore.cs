// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Squidex.Hosting;

namespace Squidex.AI.SemanticKernel.Mongo;

public sealed class MongoChatStore : IChatStore, IInitializable
{
    private readonly IMongoCollection<MongoChatEntity> collection;

    public MongoChatStore(IMongoDatabase database, IOptions<MongoChatStoreOptions> options)
    {
        collection = database.GetCollection<MongoChatEntity>(options.Value.CollectionName);
    }

    public Task InitializeAsync(
        CancellationToken ct)
    {
        return collection.Indexes.CreateOneAsync(
            new CreateIndexModel<MongoChatEntity>(
                Builders<MongoChatEntity>.IndexKeys.Ascending(x => x.Expires),
                new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.Zero
                }),
            cancellationToken: ct);
    }

    public Task RemoveAsync(string conversationId,
        CancellationToken ct)
    {
        return collection.DeleteOneAsync(x => x.Id == conversationId, ct);
    }

    public async Task<string?> GetAsync(string conversationId,
        CancellationToken ct)
    {
        var result = await collection.Find(x => x.Id == conversationId).FirstOrDefaultAsync(ct);

        return result?.Value;
    }

    public Task StoreAsync(string conversationId, string value, DateTime expires,
        CancellationToken ct)
    {
        return collection.UpdateOneAsync(x => x.Id == conversationId,
            Builders<MongoChatEntity>.Update
                .SetOnInsert(x => x.Id, conversationId)
                .Set(x => x.Value, value)
                .Set(x => x.Expires, expires),
            new UpdateOptions
            {
                IsUpsert = true
            },
            ct);
    }
}
