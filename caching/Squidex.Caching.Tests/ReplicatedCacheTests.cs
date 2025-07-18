﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Squidex.Messaging;

namespace Squidex.Caching;

public class ReplicatedCacheTests
{
    private readonly IMessageBus pubSub = A.Fake<IMessageBus>();
    private readonly ReplicatedCache sut;

    public ReplicatedCacheTests()
    {
        sut = new ReplicatedCache(CreateMemoryCache(), pubSub);
    }

    [Fact]
    public async Task Should_serve_from_cache()
    {
        await sut.AddAsync("Key", 1, TimeSpan.FromMinutes(10));

        AssertCache(sut, "Key", 1, true);

        await sut.RemoveAsync("Key");

        AssertCache(sut, "Key", null, false);
    }

    [Fact]
    public async Task Should_serve_from_cache_when_many_added()
    {
        await sut.AddAsync(
            [
                new KeyValuePair<string, object?>("Key1", 1),
                new KeyValuePair<string, object?>("Key2", 1),
            ],
            TimeSpan.FromMinutes(10));

        AssertCache(sut, "Key1", 1, true);
        AssertCache(sut, "Key2", 1, true);

        await sut.RemoveAsync(
            [
                "Key1",
                "Key2",
            ]);

        AssertCache(sut, "Key1", null, false);
        AssertCache(sut, "Key2", null, false);
    }

    [Fact]
    public async Task Should_not_serve_from_cache_when_expiration_is_not_set()
    {
        await sut.AddAsync("Key", 1, TimeSpan.Zero);

        AssertCache(sut, "Key", null, false);
    }

    [Fact]
    public async Task Should_not_serve_from_cache_when_expired()
    {
        await sut.AddAsync("Key", 1, TimeSpan.FromMilliseconds(1));

        await Task.Delay(100);

        AssertCache(sut, "Key", null, false);
    }

    [Fact]
    public async Task Should_not_invalidate_other_instances_when_added()
    {
        await sut.AddAsync("Key", 1, TimeSpan.FromHours(1));

        A.CallTo(() => pubSub.PublishAsync(A<object>._, null, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Should_send_invalidation_message_when_removed()
    {
        await sut.RemoveAsync("Key");

        A.CallTo(() => pubSub.PublishAsync(A<CacheInvalidateMessage>.That.Matches(x => x.Keys.Contains("Key")), null, A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_invalidate_keys_when_message_received()
    {
        await sut.AddAsync("Key", 1, TimeSpan.FromHours(1));
        await sut.HandleAsync(new CacheInvalidateMessage { Keys = ["Key"] }, default);

        AssertCache(sut, "Key", null, false);
    }

    [Fact]
    public async Task Should_invalidate_keys_when_message_received_from_same_instance()
    {
        await sut.AddAsync("Key", 1, TimeSpan.FromHours(1));
        await sut.HandleAsync(new CacheInvalidateMessage { Keys = ["Key"], Source = sut.InstanceId }, default);

        AssertCache(sut, "Key", 1, true);
    }

    [Fact]
    public async Task Should_handle_invalidation_message_without_keys()
    {
        await sut.HandleAsync(new CacheInvalidateMessage { }, default);
    }

    private static void AssertCache(ReplicatedCache cache, string key, object? expectedValue, bool expectedFound)
    {
        var found = cache.TryGetValue(key, out var value);

        Assert.Equal(expectedFound, found);
        Assert.Equal(expectedValue, value);
    }

    private static MemoryCache CreateMemoryCache()
    {
        return new MemoryCache(Options.Create(new MemoryCacheOptions()));
    }
}
