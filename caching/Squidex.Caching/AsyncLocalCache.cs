// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;

#pragma warning disable CS8601 // Possible null reference assignment.

namespace Squidex.Caching;

public sealed class AsyncLocalCache : ILocalCache
{
    private static readonly AsyncLocal<ConcurrentDictionary<object, object>> LocalCache = new AsyncLocal<ConcurrentDictionary<object, object>>();
    private static readonly AsyncLocalCleaner<ConcurrentDictionary<object, object>> Cleaner;

    static AsyncLocalCache()
    {
        Cleaner = new AsyncLocalCleaner<ConcurrentDictionary<object, object>>(LocalCache);
    }

    public IDisposable StartContext()
    {
        LocalCache.Value = new ConcurrentDictionary<object, object>();

        return Cleaner;
    }

    public void Add(object key, object? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        var cacheKey = GetCacheKey(key);
        var cacheLocal = LocalCache.Value;

        if (cacheLocal != null)
        {
            cacheLocal[cacheKey] = value;
        }
    }

    public void Remove(object key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var cacheKey = GetCacheKey(key);
        var cacheLocal = LocalCache.Value;

        cacheLocal?.TryRemove(cacheKey, out _);
    }

    public bool TryGetValue(object key, out object? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        value = null;

        var cacheKey = GetCacheKey(key);
        var cacheLocal = LocalCache.Value;

        return cacheLocal?.TryGetValue(cacheKey, out value) ?? false;
    }

    private static string GetCacheKey(object key)
    {
        return $"CACHE_{key}";
    }
}
