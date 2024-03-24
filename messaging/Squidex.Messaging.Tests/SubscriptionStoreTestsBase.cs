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
    private readonly string key1 = $"queue1_{Guid.NewGuid()}";
    private readonly string key2 = $"queue2_{Guid.NewGuid()}";
    private readonly string group = $"topic_{Guid.NewGuid()}";
    private DateTime now = DateTime.UtcNow;

    [Fact]
    public async Task Should_subscribe()
    {
        await using var app = await CreateSutAsync();

        await app.Sut.StoreAsync(group, key1, new TestValue(key1), TimeSpan.FromDays(30), default);
        await app.Sut.StoreAsync(group, key2, new TestValue(key2), TimeSpan.FromDays(30), default);

        var subscriptions = await app.Sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_unsubscribe()
    {
        await using var app = await CreateSutAsync();

        await app.Sut.StoreAsync(group, key1, new TestValue(key1), TimeSpan.FromDays(30), default);
        await app.Sut.StoreAsync(group, key2, new TestValue(key2), TimeSpan.FromDays(30), default);

        await app.Sut.DeleteAsync(group, key1, default);

        var subscriptions = await app.Sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_not_return_expired_subscriptions()
    {
        await using var app = await CreateSutAsync();

        await app.Sut.StoreAsync(group, key1, new TestValue(key1), TimeSpan.FromDays(30), default);
        await app.Sut.StoreAsync(group, key2, new TestValue(key2), TimeSpan.FromSeconds(30), default);

        now = now.AddDays(1);

        var subscriptions = await app.Sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_update_expiration()
    {
        await using var app = await CreateSutAsync();

        await app.Sut.StoreAsync(group, key1, new TestValue(key1), TimeSpan.FromDays(30), default);
        await app.Sut.StoreAsync(group, key2, new TestValue(key2), TimeSpan.FromDays(1), default);

        // Does not expires because last activity is in the future.
        now = now.AddDays(2);

        await app.Sut.UpdateAliveAsync(default);

        var subscriptions = await app.Sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_cleanup_subscriptions()
    {
        await using var app = await CreateSutAsync();

        await app.Sut.StoreAsync(group, key1, new TestValue(key1), TimeSpan.FromDays(30), default);
        await app.Sut.StoreAsync(group, key2, new TestValue(key2), TimeSpan.FromDays(10), default);

        // Expires in the future to force expiration.
        now = now.AddDays(20);

        var subscriptions = await app.Sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    [Fact]
    public async Task Should_not_cleanup_subscriptions_that_never_expires()
    {
        await using var app = await CreateSutAsync();

        await app.Sut.StoreAsync(group, key1, new TestValue(key1), TimeSpan.Zero, default);
        await app.Sut.StoreAsync(group, key2, new TestValue(key2), TimeSpan.Zero, default);

        // Expires in the future to force expiration.
        now = now.AddDays(30);

        var subscriptions = await app.Sut.GetEntriesAsync<TestValue>(group, default);

        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Key));
        SetEquals(new[] { key1, key2 }.ToHashSet(), subscriptions.Select(x => x.Value.Value));
    }

    private Task<Provider<IMessagingDataProvider>> CreateSutAsync(
        Action<MessagingOptions>? configure = null)
    {
        var clock = A.Fake<TimeProvider>();

        A.CallTo(() => clock.GetUtcNow())
            .ReturnsLazily(() => new DateTimeOffset(now, default));

        var serviceProvider =
            new ServiceCollection()
                .AddLogging(options =>
                {
                    options.AddDebug();
                    options.AddConsole();
                })
                .AddMessaging()
                    .Configure(configure)
                    .AddSubscriptions()
                    .AddOverride(Configure)
                    .Services
                .AddSingleton(clock)
                .BuildServiceProvider();

        return serviceProvider.CreateAsync<IMessagingDataProvider>();
    }

    protected abstract void Configure(MessagingBuilder builder);

    private static void SetEquals<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        Assert.Equal(expected.OrderBy(x => x).ToArray(), actual.OrderBy(x => x).ToArray());
    }
}
