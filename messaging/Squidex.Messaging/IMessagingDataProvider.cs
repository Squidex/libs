// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging;

public interface IMessagingDataProvider
{
    Task<IReadOnlyDictionary<string, T>> GetEntriesAsync<T>(string group,
        CancellationToken ct = default) where T : notnull;

    Task<IAsyncDisposable> StoreAsync<T>(string group, string key, T entry, TimeSpan expiresAfter,
        CancellationToken ct = default) where T : notnull;

    Task DeleteAsync(string group, string key,
        CancellationToken ct = default);
}
