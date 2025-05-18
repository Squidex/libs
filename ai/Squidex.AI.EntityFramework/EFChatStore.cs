// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Squidex.AI.Implementation;

namespace Squidex.AI.Mongo;

public sealed class EFChatStore<T>(IDbContextFactory<T> dbContextFactory) : IChatStore where T : DbContext
{
    public async Task RemoveAsync(string conversationId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Set<EFChatEntity>().Where(x => x.Id == conversationId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<Conversation?> GetAsync(string conversationId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Set<EFChatEntity>().Where(x => x.Id == conversationId).FirstOrDefaultAsync(ct);
        if (entity == null)
        {
            return null;
        }

        var conversation = JsonSerializer.Deserialize<Conversation>(entity.Value) ??
            throw new ChatException($"Cannot deserialize conversion with ID '{conversationId}'.");

        return conversation;
    }

    public async Task StoreAsync(string conversationId, Conversation conversation, DateTime now,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(conversation);

        var json = JsonSerializer.Serialize(conversation) ??
            throw new ChatException($"Cannot serialize conversion with ID '{conversationId}'.");

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Set<EFChatEntity>().Where(x => x.Id == conversationId).FirstOrDefaultAsync(ct);
        if (entity != null)
        {
            entity.LastUpdated = now;
            entity.Version = Guid.NewGuid();
            entity.Value = json;
        }
        else
        {
            entity = new EFChatEntity
            {
                Id = conversationId,
                LastUpdated = now,
                Version = Guid.NewGuid(),
                Value = json,
            };

            await dbContext.Set<EFChatEntity>().AddAsync(entity, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    public async IAsyncEnumerable<(string Id, Conversation Value)> QueryAsync(DateTime olderThan,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var records = dbContext.Set<EFChatEntity>().Where(x => x.LastUpdated < olderThan).AsAsyncEnumerable();

        await foreach (var entity in records.WithCancellation(ct))
        {
            var conversation = JsonSerializer.Deserialize<Conversation>(entity.Value) ??
                throw new ChatException($"Cannot deserialize conversion with ID '{entity.Id}'.");

            yield return (entity.Id, conversation);
        }
    }
}
