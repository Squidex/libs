// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Caching;

public static class LocalCacheExtensions
{
    public static async Task<T> GetOrCreateAsync<T>(this ILocalCache cache, object key, Func<Task<T>> creator)
    {
        if (cache.TryGetValue(key, out var value))
        {
            if (value is T typed)
            {
                return typed;
            }
            else
            {
                return default!;
            }
        }

        var result = await creator();

        cache.Add(key, result);

        return result;
    }

    public static T GetOrCreate<T>(this ILocalCache cache, object key, Func<T> creator)
    {
        if (cache.TryGetValue(key, out var value))
        {
            if (value is T typed)
            {
                return typed;
            }
            else
            {
                return default!;
            }
        }

        var result = creator();

        cache.Add(key, result);

        return result;
    }
}
