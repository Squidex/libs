// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FakeItEasy;
using Squidex.Messaging.Implementation;
using Xunit;

namespace Squidex.Messaging;

public abstract class SubscriptionStoreTestsBase
{
    private readonly TimeProvider timeProvider = A.Fake<TimeProvider>();
    private readonly string key1 = $"queue1_{Guid.NewGuid()}";
    private readonly string key2 = $"queue2_{Guid.NewGuid()}";
    private readonly string group = $"topic_{Guid.NewGuid()}";
    private DateTime now = DateTime.UtcNow;

    private sealed class TestValue
    {
        public string Value { get; set; }
    }

    protected SubscriptionStoreTestsBase()
    {
        A.CallTo(() => timeProvider.GetUtcNow())
            .ReturnsLazily(() => new DateTimeOffset(now, default));
    }

    public abstract Task<IMessagingDataStore> CreateSubscriptionStoreAsync();

    [Fact]
    public async Task Should_subscribe()
    {
        var sut = await CreateSubscriptionProvider();

        await sut.StoreAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.FromDays(30), default);
        await sut.StoreAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.FromDays(30), default);

        var subscriptions = await sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_unsubscribe()
    {
        var sut = await CreateSubscriptionProvider();

        await sut.StoreAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.FromDays(30), default);
        await sut.StoreAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.FromDays(30), default);

        await sut.DeleteAsync(group, key1, default);

        var subscriptions = await sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_not_return_expired_subscriptions()
    {
        var sut = await CreateSubscriptionProvider();

        await sut.StoreAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.FromDays(30), default);
        await sut.StoreAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.FromSeconds(30), default);

        now = now.AddDays(1);

        var subscriptions = await sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_update_expiration()
    {
        var sut = await CreateSubscriptionProvider();

        await sut.StoreAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.FromDays(30), default);
        await sut.StoreAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.FromDays(1), default);

        // Does not expires because last activity is in the future.
        now = now.AddDays(2);

        await sut.UpdateAliveAsync(default);

        var subscriptions = await sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_cleanup_subscriptions()
    {
        var sut = await CreateSubscriptionProvider();

        await sut.StoreAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.FromDays(30), default);
        await sut.StoreAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.FromDays(10), default);

        // Expires in the future to force expiration.
        now = now.AddDays(20);

        var subscriptions = await sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_not_cleanup_subscriptions_that_never_expires()
    {
        var sut = await CreateSubscriptionProvider();

        await sut.StoreAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.Zero, default);
        await sut.StoreAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.Zero, default);

        // Expires in the future to force expiration.
        now = now.AddDays(30);

        var subscriptions = await sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    private async Task<MessagingDataProvider> CreateSubscriptionProvider()
    {
        var store = await CreateSubscriptionStoreAsync();

        var serviceProvider =
            new ServiceCollection()
                .AddLogging()
                .AddSingleton<MessagingDataProvider>()
                .AddSingleton(store)
                .AddSingleton(timeProvider)
                .AddSingleton<IMessagingSerializer>(new SystemTextJsonMessagingSerializer())
                .BuildServiceProvider();

        return serviceProvider.GetRequiredService<MessagingDataProvider>();
    }

    private static void SetEquals<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        Assert.Equal(expected.OrderBy(x => x).ToArray(), actual.OrderBy(x => x).ToArray());
    }
}
