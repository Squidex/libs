// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Squidex.AI.Implementation;

public sealed class InMemoryChatStore : IChatStore
{
    private readonly ConcurrentDictionary<string, (Conversation Conversation, DateTime LastUpdate)> values = [];

    public Task RemoveAsync(string conversationId,
        CancellationToken ct)
    {
        values.Remove(conversationId, out _);
        return Task.CompletedTask;
    }

    public Task<Conversation?> GetAsync(string conversationId,
        CancellationToken ct)
    {
        values.TryGetValue(conversationId, out var result);
        return Task.FromResult<Conversation?>(result.Conversation);
    }

    public Task StoreAsync(string conversationId, Conversation conversation, DateTime now,
        CancellationToken ct)
    {
        values[conversationId] = (conversation, now);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<(string Id, Conversation Value)> QueryAsync(DateTime olderThan,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await Task.Yield();

        foreach (var (key, value) in values)
        {
            if (value.LastUpdate < olderThan)
            {
                yield return (key, value.Conversation);
            }
        }
    }
}
