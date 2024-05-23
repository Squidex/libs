// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Squidex.Hosting;

namespace Squidex.AI.Implementation.Mongo;

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
                Builders<MongoChatEntity>.IndexKeys.Ascending(x => x.LastUpdated)),
            cancellationToken: ct);
    }

    public Task RemoveAsync(string conversationId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        return collection.DeleteOneAsync(x => x.Id == conversationId, ct);
    }

    public async Task<Conversation?> GetAsync(string conversationId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        var document = await collection.Find(x => x.Id == conversationId).FirstOrDefaultAsync(ct);

        if (document == null)
        {
            return null;
        }

        var conversation = JsonSerializer.Deserialize<Conversation>(document.Value) ??
            throw new ChatException($"Cannot deserialize conversion with ID '{conversationId}'.");

        return conversation;
    }

    public Task StoreAsync(string conversationId, Conversation conversation,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(conversation);

        var json = JsonSerializer.Serialize(conversation) ??
            throw new ChatException($"Cannot serialize conversion with ID '{conversationId}'.");

        return collection.UpdateOneAsync(x => x.Id == conversationId,
            Builders<MongoChatEntity>.Update
                .SetOnInsert(x => x.Id, conversationId)
                .Set(x => x.Value, json)
                .Set(x => x.LastUpdated, DateTime.UtcNow),
            new UpdateOptions
            {
                IsUpsert = true
            },
            ct);
    }

    public async IAsyncEnumerable<(string Id, Conversation Value)> QueryAsync(DateTime olderThan,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var cursor = await collection.Find(x => x.LastUpdated < olderThan).ToCursorAsync(ct);

        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var document in cursor.Current)
            {
                var conversation = JsonSerializer.Deserialize<Conversation>(document.Value) ??
                    throw new ChatException($"Cannot deserialize conversion with ID '{document.Id}'.");

                yield return (document.Id, conversation);
            }
        }
    }
}
