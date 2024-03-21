// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Reactive.Linq;
using Squidex.Hosting;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Subscriptions;
using Squidex.Messaging.Subscriptions.Implementation;
using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable MA0040 // Flow the cancellation token

namespace Squidex.Messaging;

[Trait("Category", "Dependencies")]
public class SubscriptionServiceTests : IClassFixture<MongoFixture>
{
    private readonly string groupName = $"group-{Guid.NewGuid()}";
    private readonly string key = $"key-{Guid.NewGuid()}";

    public MongoFixture _ { get; }

    public SubscriptionServiceTests(MongoFixture fixture)
    {
        _ = fixture;
    }

    [Fact]
    public async Task Should_subscribe_hot()
    {
        var sut = await CreateSubscriptionServiceAsync();

        await sut.SubscribeAsync<TestTypes.Message>(key);

        Assert.True(await sut.HasSubscriptionsAsync<TestTypes.Message>(key));
    }

    [Fact]
    public async Task Should_subscribe_not_twice()
    {
        var sut = await CreateSubscriptionServiceAsync();

        (await sut.SubscribeAsync<TestTypes.Message>(key)).Subscribe();

        Assert.True(await sut.HasSubscriptionsAsync<TestTypes.Message>(key));
    }

    [Fact]
    public async Task Should_unsubscribe()
    {
        var sut = await CreateSubscriptionServiceAsync();

        using ((await sut.SubscribeAsync<TestTypes.Message>(key)).Subscribe())
        {
            Assert.True(await WaitForSubscriptions(sut, true));
        }

        Assert.False(await WaitForSubscriptions(sut, false));
    }

    [Fact]
    public async Task Should_load_existing_subscriptions_from_db()
    {
        var sut1 = await CreateSubscriptionServiceAsync();

        using ((await sut1.SubscribeAsync<TestTypes.Message>(key)).Subscribe())
        {
            var sut2 = await CreateSubscriptionServiceAsync();

            Assert.True(await sut2.HasSubscriptionsAsync<TestTypes.Message>(key));
        }
    }

    [Fact]
    public async Task Should_synchronize_subscriptions()
    {
        var sut1 = await CreateSubscriptionServiceAsync();
        var sut2 = await CreateSubscriptionServiceAsync();

        using ((await sut1.SubscribeAsync<TestTypes.Message>(key)).Subscribe())
        {
            Assert.True(await WaitForSubscriptions(sut1, true));
            Assert.True(await WaitForSubscriptions(sut2, true));
        }

        await Task.Delay(200);

        Assert.False(await WaitForSubscriptions(sut1, false));
        Assert.False(await WaitForSubscriptions(sut2, false));
    }

    [Fact]
    public async Task Should_subscribe()
    {
        var sut = await CreateSubscriptionServiceAsync();

        using ((await sut.SubscribeAsync<TestTypes.Message>(key)).Subscribe())
        {
            Assert.True(await sut.HasSubscriptionsAsync<TestTypes.Message>(key));
        }

        await Task.Delay(200);

        Assert.False(await sut.HasSubscriptionsAsync<TestTypes.Message>(key));
    }

    [Fact]
    public async Task Should_publish_to_self()
    {
        var sut = await CreateSubscriptionServiceAsync();

        var received = Completion();

        using ((await sut.SubscribeAsync<TestTypes.Message>(key)).Subscribe(x =>
        {
            received.TrySetResult(x);
        }))
        {
            while (received.Task.Status is not TaskStatus.Faulted and not TaskStatus.RanToCompletion)
            {
                await sut.PublishAsync(key, new TestTypes.Message(Guid.NewGuid(), 42));
                await Task.Delay(100);
            }
        }

        Assert.Equal(42, (await received.Task).Value);
    }

    [Fact]
    public async Task Should_publish_to_other_instances()
    {
        var sut1 = await CreateSubscriptionServiceAsync();
        var sut2 = await CreateSubscriptionServiceAsync();

        var received = Completion();

        using ((await sut1.SubscribeAsync<TestTypes.Message>(key)).Subscribe(x =>
        {
            received.TrySetResult(x);
        }))
        {
            while (received.Task.Status is not TaskStatus.Faulted and not TaskStatus.RanToCompletion)
            {
                await sut2.PublishAsync(key, new TestTypes.Message(Guid.NewGuid(), 42));
                await Task.Delay(100);
            }
        }

        Assert.Equal(42, (await received.Task).Value);
    }

    [Fact]
    public async Task Should_publish_to_other_instances_with_wrapper()
    {
        var sut1 = await CreateSubscriptionServiceAsync();
        var sut2 = await CreateSubscriptionServiceAsync();

        var received = Completion();

        using ((await sut1.SubscribeAsync<TestTypes.Message>(key)).Subscribe(x =>
        {
            received.TrySetResult(x);
        }))
        {
            while (received.Task.Status is not TaskStatus.Faulted and not TaskStatus.RanToCompletion)
            {
                await sut2.PublishWrapperAsync(key, new Wrapper());
                await Task.Delay(100);
            }
        }

        Assert.Equal(42, (await received.Task).Value);
    }

    private sealed class Wrapper : IPayloadWrapper<TestTypes.Message>
    {
        public TestTypes.Message Message { get; } = new TestTypes.Message(Guid.NewGuid(), 42);

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
            if ((await sut.HasSubscriptionsAsync<TestTypes.Message>(key)) == expected)
            {
                return expected;
            }

            await Task.Delay(100);
        }

        return !expected;
    }

    private static TaskCompletionSource<TestTypes.Message> Completion()
    {
        var completion = new TaskCompletionSource<TestTypes.Message>();
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

    private async Task<ISubscriptionService> CreateSubscriptionServiceAsync()
    {
        var serviceProvider =
            new ServiceCollection()
                .AddLogging()
                .AddSingleton(_.Database)
                .AddReplicatedCache()
                .AddReplicatedCacheMessaging()
                .AddMessaging()
                .AddMessagingSubscriptions()
                .AddMongoMessagingDataStore(TestHelpers.Configuration)
                .AddMongoTransport(TestHelpers.Configuration)
                .AddSingleton<IInstanceNameProvider, RandomInstanceNameProvider>()
                .Configure<MessagingOptions>(options =>
                {
                    options.DataCacheDuration = TimeSpan.Zero;
                })
                .Configure<SubscriptionOptions>(options =>
                {
                    options.GroupName = groupName;
                })
                .BuildServiceProvider();

        foreach (var initializable in serviceProvider.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await initializable.InitializeAsync(default);
        }

        foreach (var process in serviceProvider.GetRequiredService<IEnumerable<IBackgroundProcess>>())
        {
            await process.StartAsync(default);
        }

        return serviceProvider.GetRequiredService<ISubscriptionService>();
    }
}
