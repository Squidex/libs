// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Caching.Memory;
using Squidex.Messaging;

namespace Squidex.Caching;

public sealed class ReplicatedCache : IReplicatedCache, IMessageHandler<CacheInvalidateMessage>
{
    private readonly IMemoryCache memoryCache;
    private readonly IMessageBus messageBus;

    public Guid InstanceId { get; } = Guid.NewGuid();

    public ReplicatedCache(IMemoryCache memoryCache, IMessageBus messageBus)
    {
        this.memoryCache = memoryCache;
        this.messageBus = messageBus;
    }

    public Task HandleAsync(CacheInvalidateMessage message,
        CancellationToken ct)
    {
        if (message.Keys != null && message.Source != InstanceId)
        {
            foreach (var key in message.Keys)
            {
                if (key != null)
                {
                    memoryCache.Remove(key);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task AddAsync(string key, object? value, TimeSpan expiration,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (expiration <= TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }

        memoryCache.Set(key, value, expiration);

        return Task.CompletedTask;
    }

    public Task AddAsync(IEnumerable<KeyValuePair<string, object?>> items, TimeSpan expiration,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (expiration <= TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }

        foreach (var (key, value) in items)
        {
            memoryCache.Set(key, value, expiration);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        return RemoveAsync([key], ct);
    }

    public Task RemoveAsync(string key1, string key2,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key1);
        ArgumentNullException.ThrowIfNull(key2);

        return RemoveAsync([key1, key2], ct);
    }

    public async Task RemoveAsync(string[] keys,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            if (key != null)
            {
                memoryCache.Remove(key);
            }
        }

        await InvalidateAsync(keys, ct);
    }

    public bool TryGetValue(string key, out object? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        return memoryCache.TryGetValue(key, out value);
    }

    private Task InvalidateAsync(string[] keys,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(keys);

        return messageBus.PublishAsync(new CacheInvalidateMessage { Keys = keys, Source = InstanceId }, ct: ct);
    }
}
