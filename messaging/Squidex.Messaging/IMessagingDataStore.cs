// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Messaging;

public record struct Entry(string Group, string Key, SerializedObject Value, DateTime Expiration);

public interface IMessagingDataStore
{
    Task<IReadOnlyList<Entry>> GetEntriesAsync(string group,
        CancellationToken ct);

    Task StoreManyAsync(Entry[] entries,
        CancellationToken ct);

    Task DeleteAsync(string group, string key,
        CancellationToken ct);

    /// <summary>
    /// Deletes several keys of the same group. Implementations should override this to use a single
    /// statement, the default implementation just deletes the keys one after the other.
    /// </summary>
    async Task DeleteManyAsync(string group, IReadOnlyList<string> keys,
        CancellationToken ct)
    {
        foreach (var key in keys)
        {
            await DeleteAsync(group, key, ct);
        }
    }
}
