// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Reactive.Linq;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Internal;
using Squidex.Messaging.Subscriptions;
using TestHelpers;

#pragma warning disable MA0040 // Flow the cancellation token

namespace Squidex.Messaging;

[Collection(MongoMessagingCollection.Name)]
public class SubscriptionServiceTests(MongoMessagingFixture fixture)
{
    private readonly string groupName = $"group-{Guid.NewGuid()}";
    private readonly string key = $"key-{Guid.NewGuid()}";

    [Fact]
    public async Task Should_subscribe_hot()
    {
        await using var app = await CreateSutAsync();

        await app.Sut.SubscribeAsync(key);

        Assert.True(await app.Sut.HasSubscriptionsAsync(key));
    }

    [Fact]
    public async Task Should_subscribe_not_twice()
    {
        await using var app = await CreateSutAsync();

        (await app.Sut.SubscribeAsync(key)).Subscribe();

        Assert.True(await app.Sut.HasSubscriptionsAsync(key));
    }

    [Fact]
    public async Task Should_unsubscribe()
    {
        await using var app = await CreateSutAsync();

        using ((await app.Sut.SubscribeAsync(key)).Subscribe())
        {
            Assert.True(await WaitForSubscriptions(app.Sut, true));
        }

        Assert.False(await WaitForSubscriptions(app.Sut, false));
    }

    [Fact]
    public async Task Should_load_existing_subscriptions_from_db()
    {
        await using var app = await CreateSutAsync();

        using ((await app.Sut.SubscribeAsync(key)).Subscribe())
        {
            await using var app2 = await CreateSutAsync();

            Assert.True(await app.Sut.HasSubscriptionsAsync(key));
        }
    }

    [Fact]
    public async Task Should_synchronize_subscriptions()
    {
        await using var app1 = await CreateSutAsync();
        await using var app2 = await CreateSutAsync();

        using ((await app1.Sut.SubscribeAsync(key)).Subscribe())
        {
            Assert.True(await WaitForSubscriptions(app1.Sut, true));
            Assert.True(await WaitForSubscriptions(app2.Sut, true));
        }

        await Task.Delay(200);

        Assert.False(await WaitForSubscriptions(app1.Sut, false));
        Assert.False(await WaitForSubscriptions(app2.Sut, false));
    }

    [Fact]
    public async Task Should_subscribe()
    {
        await using var app = await CreateSutAsync();

        using ((await app.Sut.SubscribeAsync(key)).Subscribe())
        {
            Assert.True(await app.Sut.HasSubscriptionsAsync(key));
        }

        await Task.Delay(200);

        Assert.False(await app.Sut.HasSubscriptionsAsync(key));
    }

    [Fact]
    public async Task Should_publish_to_self()
    {
        await using var app = await CreateSutAsync();

        var received = Completion();

        using ((await app.Sut.SubscribeAsync(key)).Subscribe(x =>
        {
            received.TrySetResult(x);
        }))
        {
            while (received.Task.Status is not TaskStatus.Faulted and not TaskStatus.RanToCompletion)
            {
                await app.Sut.PublishAsync(key, new TestMessage(Guid.NewGuid(), 42));
                await Task.Delay(100);
            }
        }

        Assert.Equal(42, (await received.Task as TestMessage)?.Value);
    }

    [Fact]
    public async Task Should_publish_to_other_instances()
    {
        await using var app1 = await CreateSutAsync();
        await using var app2 = await CreateSutAsync();

        var received = Completion();

        using ((await app1.Sut.SubscribeAsync(key)).Subscribe(x =>
        {
            received.TrySetResult(x);
        }))
        {
            while (received.Task.Status is not TaskStatus.Faulted and not TaskStatus.RanToCompletion)
            {
                await app2.Sut.PublishAsync(key, new TestMessage(Guid.NewGuid(), 42));
                await Task.Delay(100);
            }
        }

        Assert.Equal(42, (await received.Task as TestMessage)?.Value);
    }

    [Fact]
    public async Task Should_publish_to_other_instances_with_wrapper()
    {
        await using var app1 = await CreateSutAsync();
        await using var app2 = await CreateSutAsync();

        var received = Completion();

        using ((await app1.Sut.SubscribeAsync(key)).Subscribe(x =>
        {
            received.TrySetResult(x);
        }))
        {
            while (received.Task.Status is not TaskStatus.Faulted and not TaskStatus.RanToCompletion)
            {
                await app2.Sut.PublishAsync(key, new Wrapper());
                await Task.Delay(100);
            }
        }

        Assert.Equal(42, (await received.Task as TestMessage)?.Value);
    }

    private sealed class Wrapper : IPayloadWrapper
    {
        public object Message { get; } = new TestMessage(Guid.NewGuid(), 42);

        public ValueTask<object> CreatePayloadAsync()
        {
            return new ValueTask<object>(Message);
        }
    }

    private async Task<bool> WaitForSubscriptions(ISubscriptionService sut, bool expected)
    {
        using var cts = new CancellationTokenSource(30_000);

        while (!cts.IsCancellationRequested)
        {
            if ((await sut.HasSubscriptionsAsync(key)) == expected)
            {
                return expected;
            }

            await Task.Delay(100);
        }

        return !expected;
    }

    private static TaskCompletionSource<object> Completion()
    {
        var completion = new TaskCompletionSource<object>();
        var cancelled = new CancellationTokenSource(30_000);

        var registration = cancelled.Token.Register(() =>
        {
            completion.TrySetCanceled();
        });
#pragma warning disable MA0134 // Observe result of async calls
        completion.Task.ContinueWith(x =>
        {
            cancelled.Dispose();
        });
#pragma warning restore MA0134 // Observe result of async calls

        return completion;
    }

    private Task<Provider<ISubscriptionService>> CreateSutAsync()
    {
        var serviceProvider =
            new ServiceCollection()
                .AddLogging(options =>
                {
                    options.AddDebug();
                    options.AddConsole();
                })
                .AddSingleton(fixture.MongoDatabase)
                .AddReplicatedCache()
                .AddMessaging()
                    .Configure<MessagingOptions>(options =>
                    {
                        options.DataCacheDuration = TimeSpan.Zero;
                    })
                    .Configure<SubscriptionOptions>(options =>
                    {
                        options.GroupName = groupName;
                    })
                    .AddSubscriptions()
                    .AddMongoDataStore(TestUtils.Configuration)
                    .AddMongoTransport(TestUtils.Configuration)
                    .Services
                .AddSingleton<IInstanceNameProvider, RandomInstanceNameProvider>()
                .BuildServiceProvider();

        return serviceProvider.CreateAsync<ISubscriptionService>();
    }
}
