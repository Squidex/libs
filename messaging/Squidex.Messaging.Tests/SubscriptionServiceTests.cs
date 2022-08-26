// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Subscriptions;
using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable MA0040 // Flow the cancellation token

namespace Squidex.Messaging
{
    [Trait("Category", "Dependencies")]
    public class SubscriptionServiceTests : IClassFixture<MongoFixture>
    {
        private readonly string groupName = $"group-{Guid.NewGuid()}";

        public MongoFixture _ { get; }

        public SubscriptionServiceTests(MongoFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public async Task Should_subscribe_lazily()
        {
            var sut = await CreateSubscriptionServiceAsync();

            sut.Subscribe<object>();

            Assert.False(sut.HasSubscriptions);
        }

        [Fact]
        public async Task Should_load_existing_subscriptions_from_db()
        {
            var sut1 = await CreateSubscriptionServiceAsync();

            using (var subscription = sut1.Subscribe<object>().Subscribe(x =>
            {
                // Just subscribe.
            }))
            {
                var sut2 = await CreateSubscriptionServiceAsync();

                Assert.True(sut2.HasSubscriptions);
            }
        }

        [Fact]
        public async Task Should_synchronize_subscriptions()
        {
            var sut1 = await CreateSubscriptionServiceAsync();
            var sut2 = await CreateSubscriptionServiceAsync();

            using (var subscription = sut1.Subscribe<object>().Subscribe(x =>
            {
                // Just subscribe.
            }))
            {
                Assert.True(await WaitForSubscriptions(sut1, true));
                Assert.True(await WaitForSubscriptions(sut2, true));
            }

            Assert.False(await WaitForSubscriptions(sut1, false));
            Assert.False(await WaitForSubscriptions(sut2, false));
        }

        [Fact]
        public async Task Should_subscribe()
        {
            var sut = await CreateSubscriptionServiceAsync();

            using (var subscription = sut.Subscribe<object>().Subscribe(x =>
            {
                // Just subscribe.
            }))
            {
                Assert.True(sut.HasSubscriptions);
            }

            Assert.False(sut.HasSubscriptions);
        }

        [Fact]
        public async Task Should_publish_to_self()
        {
            var sut = await CreateSubscriptionServiceAsync();

            var received = Completion();

            using (var subscription = sut.Subscribe<TestTypes.Message>().Subscribe(x =>
            {
                received.TrySetResult(x);
            }))
            {
                while (received.Task.Status is not TaskStatus.Faulted and not TaskStatus.RanToCompletion)
                {
                    await sut.PublishAsync(new TestTypes.Message(Guid.NewGuid(), 42));

                    await Task.Delay(100);
                }
            }

            Assert.Equal(42, (await received.Task).Value);
        }

        [Fact]
        public async Task Should_publish_to_self_using_messaging()
        {
            var sut = await CreateSubscriptionServiceAsync(true);

            var received = Completion();

            using (var subscription = sut.Subscribe<TestTypes.Message>().Subscribe(x =>
            {
                received.TrySetResult(x);
            }))
            {
                while (received.Task.Status is not TaskStatus.Faulted and not TaskStatus.RanToCompletion)
                {
                    await sut.PublishAsync(new TestTypes.Message(Guid.NewGuid(), 42));

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

            using (var subscription = sut1.Subscribe<TestTypes.Message>().Subscribe(x =>
            {
                received.TrySetResult(x);
            }))
            {
                while (received.Task.Status is not TaskStatus.Faulted and not TaskStatus.RanToCompletion)
                {
                    await sut2.PublishAsync(new TestTypes.Message(Guid.NewGuid(), 42));

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

            using (var subscription = sut1.Subscribe<TestTypes.Message>().Subscribe(x =>
            {
                received.TrySetResult(x);
            }))
            {
                while (received.Task.Status is not TaskStatus.Faulted and not TaskStatus.RanToCompletion)
                {
                    await sut2.PublishAsync(new Wrapper());

                    await Task.Delay(100);
                }
            }

            Assert.Equal(42, (await received.Task).Value);
        }

        private sealed class Wrapper : IPayloadWrapper
        {
            public object Message { get; } = new TestTypes.Message(Guid.NewGuid(), 42);

            public ValueTask<object> CreatePayloadAsync()
            {
                return new ValueTask<object>(Message);
            }
        }

        private static async Task<bool> WaitForSubscriptions(ISubscriptionService sut, bool expected)
        {
            using var cts = new CancellationTokenSource(30_000);

            while (!cts.IsCancellationRequested)
            {
                if (sut.HasSubscriptions == expected)
                {
                    return expected;
                }

                await Task.Delay(100);
            }

            return !expected;
        }

        private static TaskCompletionSource<TestTypes.Message> Completion()
        {
            var received = new TaskCompletionSource<TestTypes.Message>();

            var cts = new CancellationTokenSource(30_000);

            var registration = cts.Token.Register(() =>
            {
                received.TrySetCanceled();
            });

            received.Task.ContinueWith(x =>
            {
                cts.Dispose();
            });

            return received;
        }

        private async Task<ISubscriptionService> CreateSubscriptionServiceAsync(bool sendToSelf = false)
        {
            var serviceProvider =
                new ServiceCollection()
                    .AddLogging()
                    .AddSingleton(_.Database)
                    .AddMessaging()
                    .AddMessagingSubscriptions()
                    .AddMongoTransport(TestHelpers.Configuration)
                    .AddSingleton<IInstanceNameProvider, RandomInstanceNameProvider>()
                    .Configure<SubscriptionOptions>(options =>
                    {
                        options.GroupName = groupName;
                        options.SendMessagesToSelf = sendToSelf;
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
}
