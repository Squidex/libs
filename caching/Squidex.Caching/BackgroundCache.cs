// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Squidex.Caching;

public sealed class BackgroundCache(
    IMemoryCache memoryCache,
    TimeProvider timeProvider,
    IOptions<BackgroundCacheOptions> options,
    ILogger<BackgroundCache> log)
    : IBackgroundCache
{
    private static readonly TimeSpan MaxComputionTime = TimeSpan.FromMinutes(2);

    // Creating values is expensive, so failures are retried with a delay. Otherwise a persistent
    // error would rebuild the value for every request.
    private readonly TimeSpan retryDelay = options.Value.RetryDelay;
    private readonly StripedLock locks = new StripedLock(32);
    private readonly ConcurrentDictionary<object, bool> isUpdating = new ConcurrentDictionary<object, bool>();

    private sealed class Entry<T>(Task<T> value, DateTimeOffset expires)
    {
        // Ticks instead of DateTimeOffset, so that it can be read and written atomically.
        private long retryAfter;

        public Task<T> Value { get; } = value;

        public DateTimeOffset Expires { get; } = expires;

        public bool IsFailed => Value.IsFaulted || Value.IsCanceled;

        public void BlockRetry(DateTimeOffset until)
        {
            Volatile.Write(ref retryAfter, until.UtcTicks);
        }

        public bool IsRetryBlocked(DateTimeOffset now)
        {
            return Volatile.Read(ref retryAfter) > now.UtcTicks;
        }

        public bool CanRecreate(DateTimeOffset now)
        {
            // A failed creation is served until the retry delay has passed, but not until the entry
            // expires. Otherwise a single failure would poison the key for the whole expiration.
            return IsFailed && !IsRetryBlocked(now);
        }
    }

    public Task<T> GetOrCreateAsync<T>(object key, TimeSpan expiration, Func<object, Task<T>> creator, Func<T, Task<bool>>? isValid = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(creator);

        var now = timeProvider.GetUtcNow();

        if (memoryCache.TryGetValue(key, out var cached))
        {
            var entry = GetEntry<T>(cached);
            if (!entry.CanRecreate(now))
            {
                RefreshIfNeeded(entry, key, expiration, now, isValid, creator);
                return entry.Value;
            }
        }

        lock (locks.Get(key))
        {
            if (memoryCache.TryGetValue(key, out var cached2))
            {
                var entry = GetEntry<T>(cached2);
                if (!entry.CanRecreate(now))
                {
                    RefreshIfNeeded(entry, key, expiration, now, isValid, creator);
                    return entry.Value;
                }
            }

            var newEntry = AddEntry(key, expiration, creator(key), now);

            BlockRetryOnFailure(newEntry);

            return newEntry.Value;
        }
    }

    private void BlockRetryOnFailure<T>(Entry<T> entry)
    {
        entry.Value.ContinueWith(
            _ => entry.BlockRetry(timeProvider.GetUtcNow() + retryDelay),
            CancellationToken.None,
            TaskContinuationOptions.NotOnRanToCompletion |
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default).Forget();
    }

    private static Entry<T> GetEntry<T>(object? cached)
    {
        if (cached is Entry<T> entry)
        {
            return entry;
        }

        throw new InvalidOperationException("Another object with the same key but invalid type is already in the cache.");
    }

    private Entry<T> AddEntry<T>(object key, TimeSpan expiration, Task<T> value, DateTimeOffset now)
    {
        var absoluteExpiration = now + expiration;

        var newEntry = new Entry<T>(value, absoluteExpiration - MaxComputionTime);
        memoryCache.Set(key, newEntry, absoluteExpiration);

        return newEntry;
    }

    private void RefreshIfNeeded<T>(Entry<T> entry, object key, TimeSpan expiration, DateTimeOffset now, Func<T, Task<bool>>? isValid, Func<object, Task<T>> creator)
    {
        // This runs on every cache hit, so decide synchronously whether there is anything to do at
        // all. Calling the async method would allocate a state machine and a task per hit, just to
        // find out that the entry is still fresh.
        if (entry.Value.Status != TaskStatus.RanToCompletion)
        {
            return;
        }

        if (isUpdating.ContainsKey(key))
        {
            return;
        }

        if (entry.IsRetryBlocked(now))
        {
            return;
        }

        // Without a validator a fresh entry needs no work, which is the common case.
        if (entry.Expires > now && isValid == null)
        {
            return;
        }

        // The refresh outlives the caller and its result is shared with all other callers. Therefore
        // it must not see the async local state of the caller, such as request scoped caches.
        using (ExecutionContext.SuppressFlow())
        {
            Task.Run(() => RefreshInBackground(entry, key, expiration, now, isValid, creator)).Forget();
        }
    }

    private async Task RefreshInBackground<T>(Entry<T> entry, object key, TimeSpan expiration, DateTimeOffset now, Func<T, Task<bool>>? isValid, Func<object, Task<T>> creator)
    {
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

                AddEntry(key, expiration, Task.FromResult(newValue), timeProvider.GetUtcNow());
            }
            catch (Exception ex)
            {
                // Keep the previous value, it is better to serve outdated data than an error.
                entry.BlockRetry(timeProvider.GetUtcNow() + retryDelay);

                LogMessages.FailedToRefreshCacheEntry(log, ex, key);
            }
            finally
            {
                isUpdating.TryRemove(key, out _);
            }
        }
    }
}
