// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;

namespace Squidex.Text.ChatBots;

public sealed class InMemoryChatStore : IChatStore
{
    private readonly ConcurrentDictionary<string, string> values = new ConcurrentDictionary<string, string>();

    public Task ClearAsync(string conversationId,
        CancellationToken ct)
    {
        values.Remove(conversationId, out _);
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string conversationId,
        CancellationToken ct)
    {
        values.TryGetValue(conversationId, out var result);
        return Task.FromResult(result);
    }

    public Task StoreAsync(string conversationId, string value,
        CancellationToken ct)
    {
        values[conversationId] = value;
        return Task.CompletedTask;
    }
}
