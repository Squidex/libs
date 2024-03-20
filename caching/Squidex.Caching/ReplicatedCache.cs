// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Caching.Memory;
using Squidex.Caching;
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
        return RemoveAsync(new[] { key }, ct);
    }

    public Task RemoveAsync(string key1, string key2,
        CancellationToken ct = default)
    {
        return RemoveAsync(new[] { key1, key2 }, ct);
    }

    public async Task RemoveAsync(string[] keys,
        CancellationToken ct = default)
    {
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
        return memoryCache.TryGetValue(key, out value);
    }

    private Task InvalidateAsync(string[] keys,
        CancellationToken ct)
    {
        return messageBus.PublishAsync(new CacheInvalidateMessage { Keys = keys, Source = InstanceId }, ct: ct);
    }
}
