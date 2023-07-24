// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FakeItEasy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Squidex.Messaging;
using Xunit;

namespace Squidex.Caching;

public class ReplicatedCacheTests
{
    private readonly IMessageBus pubSub = A.Fake<IMessageBus>();
    private readonly ReplicatedCacheOptions options = new ReplicatedCacheOptions { Enable = true };
    private readonly ReplicatedCache sut;

    public ReplicatedCacheTests()
    {
        sut = new ReplicatedCache(CreateMemoryCache(), pubSub, Options.Create(options));
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
            new[]
            {
                new KeyValuePair<string, object?>("Key1", 1),
                new KeyValuePair<string, object?>("Key2", 1),
            },
            TimeSpan.FromMinutes(10));

        AssertCache(sut, "Key1", 1, true);
        AssertCache(sut, "Key2", 1, true);

        await sut.RemoveAsync(
            new[]
            {
                "Key1",
                "Key2"
            });

        AssertCache(sut, "Key1", null, false);
        AssertCache(sut, "Key2", null, false);
    }

    [Fact]
    public async Task Should_not_serve_from_cache_when_disabled()
    {
        options.Enable = false;

        await sut.AddAsync("Key", 1, TimeSpan.FromMilliseconds(100));

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
        options.Enable = true;

        await sut.RemoveAsync("Key");

        A.CallTo(() => pubSub.PublishAsync(A<CacheInvalidateMessage>.That.Matches(x => x.Keys.Contains("Key")), null, A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_not_send_invalidation_message_when_not_enabled()
    {
        options.Enable = false;

        await sut.RemoveAsync("Key");

        A.CallTo(() => pubSub.PublishAsync(A<object>._, null, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Should_invalidate_keys_when_message_received()
    {
        await sut.AddAsync("Key", 1, TimeSpan.FromHours(1));
        await sut.HandleAsync(new CacheInvalidateMessage { Keys = new[] { "Key" } }, default);

        AssertCache(sut, "Key", null, false);
    }

    [Fact]
    public async Task Should_invalidate_keys_when_message_received_from_same_instance()
    {
        await sut.AddAsync("Key", 1, TimeSpan.FromHours(1));
        await sut.HandleAsync(new CacheInvalidateMessage { Keys = new[] { "Key" }, Source = sut.InstanceId }, default);

        AssertCache(sut, "Key", 1, true);
    }

    [Fact]
    public async Task Should_handle_invalidation_message_without_keys()
    {
        await sut.HandleAsync(new CacheInvalidateMessage { }, default);
    }

    private static void AssertCache(IReplicatedCache cache, string key, object? expectedValue, bool expectedFound)
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
