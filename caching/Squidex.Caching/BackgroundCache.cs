// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Squidex.Caching
{
    public sealed class BackgroundCache : IBackgroundCache
    {
        private static readonly TimeSpan MaxComputionTime = TimeSpan.FromMinutes(2);
        private readonly object[] locks = new object[32];
        private readonly ConcurrentDictionary<object, bool> isUpdating = new ConcurrentDictionary<object, bool>();
        private readonly IMemoryCache memoryCache;

        private sealed class Entry<T>
        {
            public Task<T> Value { get; }

            public DateTimeOffset Expires { get; }

            public Entry(Task<T> value, DateTimeOffset expires)
            {
                Value = value;

                Expires = expires;
            }
        }

        public Func<DateTimeOffset>? Clock { get; set; }

        public BackgroundCache(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;

            for (var i = 0; i < locks.Length; i++)
            {
                locks[i] = new object();
            }
        }

        public Task<T> GetOrCreateAsync<T>(object key, TimeSpan expiration, Func<object, Task<T>> creator, Func<T, Task<bool>>? isValid = null)
        {
            var now = GetTime();

            if (memoryCache.TryGetValue(key, out var cached))
            {
                if (cached is Entry<T> entry)
                {
                    RefreshInBackground(entry, key, expiration, now, isValid, creator).Forget();
                    return entry.Value;
                }

                throw new InvalidOperationException("Another object with the same key but invalid type is already in the cache.");
            }

            lock (GetLock(key))
            {
                if (memoryCache.TryGetValue(key, out var cached2))
                {
                    if (cached2 is Entry<T> entry)
                    {
                        RefreshInBackground(entry, key, expiration, now, isValid, creator).Forget();
                        return entry.Value;
                    }

                    throw new InvalidOperationException("Another object with the same key but invalid type is already in the cache.");
                }

                var newEntry = AddEntry(key, expiration, creator(key), now);

                return newEntry.Value;
            }
        }

        private Entry<T> AddEntry<T>(object key, TimeSpan expiration, Task<T> value, DateTimeOffset now)
        {
            var absoluteExpiration = now + expiration;

            var newEntry = new Entry<T>(value, absoluteExpiration - MaxComputionTime);

            memoryCache.Set(key, newEntry, absoluteExpiration);

            return newEntry;
        }

        private async Task RefreshInBackground<T>(Entry<T> entry, object key, TimeSpan expiration, DateTimeOffset now, Func<T, Task<bool>>? isValid, Func<object, Task<T>> creator)
        {
            if (entry.Value.Status != TaskStatus.RanToCompletion)
            {
                return;
            }

            if (isUpdating.ContainsKey(key))
            {
                return;
            }

            var snapshot = await entry.Value;

            if (entry.Expires > now && (isValid == null || await isValid(snapshot)))
            {
                return;
            }

            if (isUpdating.TryAdd(key, true))
            {
                try
                {
                    var newValue = await creator(key);

                    AddEntry(key, expiration, Task.FromResult(newValue), GetTime());
                }
                catch (Exception ex)
                {
                    AddEntry(key, expiration, Task.FromException<T>(ex), GetTime());
                }
                finally
                {
                    isUpdating.TryRemove(key, out _);
                }
            }
        }

        private object GetLock(object key)
        {
            var index = Math.Abs(key.GetHashCode() % locks.Length);

            return locks[index];
        }

        private DateTimeOffset GetTime()
        {
            var clock = Clock;

            if (clock != null)
            {
                return clock();
            }

            return DateTimeOffset.UtcNow;
        }
    }
}
