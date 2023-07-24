// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Caching;

public interface IReplicatedCache
{
    Task AddAsync(string key, object? value, TimeSpan expiration,
        CancellationToken ct = default);

    Task AddAsync(IEnumerable<KeyValuePair<string, object?>> items, TimeSpan expiration,
        CancellationToken ct = default);

    Task RemoveAsync(string keys,
        CancellationToken ct = default);

    Task RemoveAsync(string key1, string key2,
        CancellationToken ct = default);

    Task RemoveAsync(string[] keys,
        CancellationToken ct = default);

    bool TryGetValue(string key, out object? value);
}
