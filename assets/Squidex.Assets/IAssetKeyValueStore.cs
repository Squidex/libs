// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public interface IAssetKeyValueStore<T>
{
    Task<T> GetAsync(string key,
        CancellationToken ct = default);

    Task SetAsync(string key, T value, DateTimeOffset expiration,
        CancellationToken ct = default);

    Task DeleteAsync(string key,
        CancellationToken ct = default);

    IAsyncEnumerable<(string Key, T Value)> GetExpiredEntriesAsync(DateTimeOffset now,
        CancellationToken ct = default);
}
