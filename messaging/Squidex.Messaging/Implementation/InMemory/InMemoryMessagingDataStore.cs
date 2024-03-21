// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;

namespace Squidex.Messaging.Implementation.InMemory;

public class InMemoryMessagingDataStore : IMessagingDataStore
{
    private readonly ConcurrentDictionary<(string Group, string Key), Entry> entries = [];

    public Task<IReadOnlyList<Entry>> GetEntriesAsync(string group,
        CancellationToken ct)
    {
        var result = entries.Where(x => x.Value.Group == group).Select(x => x.Value).ToList();

        return Task.FromResult<IReadOnlyList<Entry>>(result);
    }

    public Task StoreManyAsync(Entry[] entries,
        CancellationToken ct)
    {
        foreach (var entry in entries)
        {
            this.entries[(entry.Group, entry.Key)] = entry;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string group, string key,
        CancellationToken ct)
    {
        entries.TryRemove((group, key), out _);

        return Task.CompletedTask;
    }
}
