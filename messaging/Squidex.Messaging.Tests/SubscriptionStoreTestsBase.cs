// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FakeItEasy;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Internal;
using Xunit;

namespace Squidex.Messaging;

public abstract class SubscriptionStoreTestsBase
{
    private readonly IClock clock = A.Fake<IClock>();
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
        A.CallTo(() => clock.UtcNow)
            .ReturnsLazily(() => now);
    }

    public abstract Task<IMessagingSubscriptionStore> CreateSubscriptionStoreAsync();

    [Fact]
    public async Task Should_subscribe()
    {
        var sut = await CreateSubscriptionManagerAsync();

        await sut.SubscribeAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.FromDays(30), default);
        await sut.SubscribeAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.FromDays(30), default);

        var subscriptions = await sut.GetSubscriptionsAsync<TestValue>(group, default);

        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_unsubscribe()
    {
        var sut = await CreateSubscriptionManagerAsync();

        await sut.SubscribeAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.FromDays(30), default);
        await sut.SubscribeAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.FromDays(30), default);

        await sut.UnsubscribeAsync(group, key1, default);

        var subscriptions = await sut.GetSubscriptionsAsync<TestValue>(group, default);

        SetEquals(new[] { key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_not_return_expired_subscriptions()
    {
        var sut = await CreateSubscriptionManagerAsync();

        await sut.SubscribeAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.FromDays(30), default);
        await sut.SubscribeAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.FromSeconds(30), default);

        now = now.AddDays(1);

        var subscriptions = await sut.GetSubscriptionsAsync<TestValue>(group, default);

        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_update_expiration()
    {
        var sut = await CreateSubscriptionManagerAsync();

        await sut.SubscribeAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.FromDays(30), default);
        await sut.SubscribeAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.FromDays(1), default);

        // Does not expires because last activity is in the future.
        now = now.AddDays(2);

        await sut.UpdateAliveAsync(default);

        var subscriptions = await sut.GetSubscriptionsAsync<TestValue>(group, default);

        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_cleanup_subscriptions()
    {
        var sut = await CreateSubscriptionManagerAsync();

        await sut.SubscribeAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.FromDays(30), default);
        await sut.SubscribeAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.FromDays(10), default);

        // Expires in the future to force expiration.
        now = now.AddDays(20);

        await sut.CleanupAsync(default);

        var subscriptions = await sut.GetSubscriptionsAsync<TestValue>(group, default);

        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_not_cleanup_subscriptions_that_never_expires()
    {
        var sut = await CreateSubscriptionManagerAsync();

        await sut.SubscribeAsync(group, key1, new TestValue { Value = key1 }, TimeSpan.Zero, default);
        await sut.SubscribeAsync(group, key2, new TestValue { Value = key2 }, TimeSpan.Zero, default);

        // Expires in the future to force expiration.
        now = now.AddDays(30);

        await sut.CleanupAsync(default);

        var subscriptions = await sut.GetSubscriptionsAsync<TestValue>(group, default);

        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    private async Task<DefaultMessagingSubscriptions> CreateSubscriptionManagerAsync()
    {
        var store = await CreateSubscriptionStoreAsync();

        var serviceProvider =
            new ServiceCollection()
                .AddLogging()
                .AddSingleton<DefaultMessagingSubscriptions>()
                .AddSingleton(store)
                .AddSingleton(clock)
                .AddSingleton<IMessagingSerializer>(new SystemTextJsonMessagingSerializer())
                .BuildServiceProvider();

        return serviceProvider.GetRequiredService<DefaultMessagingSubscriptions>();
    }

    private static void SetEquals<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        Assert.Equal(expected.OrderBy(x => x).ToArray(), actual.OrderBy(x => x).ToArray());
    }
}
