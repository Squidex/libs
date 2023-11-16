// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using System.Globalization;
using FakeItEasy;
using Squidex.Hosting;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Internal;
using Xunit;

namespace Squidex.Messaging;

[Trait("Category", "Dependencies")]
public abstract class MessagingTestsBase
{
    private readonly Guid testIdentifier = Guid.NewGuid();

    protected abstract void ConfigureServices(IServiceCollection services, ChannelName channel, bool consume);

    protected virtual bool CanHandleAndSimulateTimeout { get; } = true;

    protected virtual string TopicOrQueueName { get; } = $"channel_topic_{Guid.NewGuid()}";

    private sealed class ExpectationHandler : IMessageHandler<TestTypes.Message>
    {
        private readonly int expectCount;
        private readonly Guid expectedId;
        private readonly TaskCompletionSource tcs = new TaskCompletionSource();
        private readonly ConcurrentBag<int> messagesReceives = [];
        private readonly CancellationTokenSource cts;

        public Task Completion => tcs.Task;

        public IEnumerable<int> MessagesReceives => messagesReceives.OrderBy(x => x);

        public ExpectationHandler(int expectCount, Guid expectedId)
        {
            this.expectCount = expectCount;
            this.expectedId = expectedId;

            cts = new CancellationTokenSource(30 * 1000);

            cts.Token.Register(() =>
            {
                _ = tcs.TrySetResult();
            });
        }

        public Task HandleAsync(TestTypes.Message message,
            CancellationToken ct)
        {
            if (message.TestId == expectedId)
            {
                messagesReceives.Add(message.Value);
            }

            if (expectCount == messagesReceives.Count)
            {
                tcs.TrySetResult();
            }

            return Task.CompletedTask;
        }
    }

    private async Task<(IAsyncDisposable, IMessageBus)> CreateMessagingAsync(ChannelName channel, IMessageHandler? handler, DateTime now,
        Action<MessagingOptions>? configure = null)
    {
        var clock = A.Fake<IClock>();

        A.CallTo(() => clock.UtcNow)
            .Returns(now);

        var serviceCollection =
            new ServiceCollection()
                .AddLogging(options =>
                {
                    options.AddDebug();
                    options.AddConsole();
                })
                .AddSingleton(clock)
                .AddSingleton<IInstanceNameProvider, RandomInstanceNameProvider>()
                .AddMessaging(options =>
                {
                    options.Routing.AddFallback(channel);

                    configure?.Invoke(options);
                });

        if (handler != null)
        {
            serviceCollection.AddSingleton(handler);
        }

        ConfigureServices(serviceCollection, channel, handler != null);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        foreach (var initializable in serviceProvider.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await initializable.InitializeAsync(default);
        }

        foreach (var process in serviceProvider.GetRequiredService<IEnumerable<IBackgroundProcess>>())
        {
            await process.StartAsync(default);
        }

        var producer = serviceProvider.GetRequiredService<IMessageBus>();

        return (new Cleanup(serviceProvider), producer);
    }

    private sealed class Cleanup : IAsyncDisposable
    {
        private readonly IServiceProvider serviceProvider;

        public Cleanup(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var initializable in serviceProvider.GetRequiredService<IEnumerable<IInitializable>>())
            {
                await initializable.ReleaseAsync(default);
            }

            (serviceProvider as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public async Task Should_throw_exception_if_no_route_found()
    {
        var consumer = new DelegatingHandler<TestTypes.Message>(message =>
        {
            return Task.CompletedTask;
        });

        var (app, bus) = await CreateMessagingAsync(new ChannelName(TopicOrQueueName), consumer, DateTime.UtcNow, options =>
        {
            options.Routing.Clear();
        });

        await using (app)
        {
            await Assert.ThrowsAnyAsync<Exception>(() => bus.PublishAsync(213));
        }
    }

    [Fact]
    public async Task Should_not_throw_exception_if_no_channel_found()
    {
        var consumer = new DelegatingHandler<TestTypes.Message>(message =>
        {
            return Task.CompletedTask;
        });

        var (app, bus) = await CreateMessagingAsync(new ChannelName(TopicOrQueueName), consumer, DateTime.UtcNow, options =>
        {
            options.Routing.Clear();
        });

        await using (app)
        {
            var message = new TestTypes.Message(Guid.NewGuid(), 10);

            await bus.PublishToChannelAsync(message, new ChannelName("invalid"));
        }
    }

    [Fact]
    public async Task Should_consume_base_classes()
    {
        var testMessages = Enumerable.Range(0, 20).ToList();
        var testHandler = new ExpectationHandler(testMessages.Count, testIdentifier);

        var (app, bus) = await CreateMessagingAsync(new ChannelName(TopicOrQueueName), testHandler, DateTime.UtcNow);

        await using (app)
        {
            foreach (var message in testMessages)
            {
                var key = message.ToString(CultureInfo.InvariantCulture);

                await bus.PublishAsync((TestTypes.BaseMessage)new TestTypes.Message(testIdentifier, message), key);
            }

            await testHandler.Completion;

            Assert.Equal(testMessages, testHandler.MessagesReceives.ToList());
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(20)]
    public async Task Should_consume_messages(int numConsumers)
    {
        var testMessages = Enumerable.Range(0, 20).ToList();
        var testHandler = new ExpectationHandler(testMessages.Count, testIdentifier);

        var apps = new List<(IAsyncDisposable App, IMessageBus Bus)>();

        for (var i = 0; i < numConsumers; i++)
        {
            apps.Add(await CreateMessagingAsync(new ChannelName(TopicOrQueueName), testHandler, DateTime.UtcNow));
        }

        try
        {
            var bus = apps[0].Bus;

            foreach (var message in testMessages)
            {
                var key = message.ToString(CultureInfo.InvariantCulture);

                await bus.PublishAsync(new TestTypes.Message(testIdentifier, message), key);
            }

            await testHandler.Completion;

            Assert.Equal(testMessages, testHandler.MessagesReceives.ToList());
        }
        finally
        {
            foreach (var (cleaner, _) in apps)
            {
                await cleaner.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task Should_bring_message_back_when_consumer_times_out()
    {
        if (!CanHandleAndSimulateTimeout)
        {
            return;
        }

        var testMessages = Enumerable.Range(0, 20).ToList();

        var consumer1 = new DelegatingHandler<TestTypes.Message>(message =>
        {
            return Task.Delay(TimeSpan.FromDays(30));
        });

        var (app1, bus1) = await CreateMessagingAsync(new ChannelName(TopicOrQueueName), consumer1, DateTime.UtcNow);

        await using (app1)
        {
            foreach (var message in testMessages)
            {
                var key = message.ToString(CultureInfo.InvariantCulture);

                await bus1.PublishAsync(new TestTypes.Message(testIdentifier, message), key);
            }
        }

        var testHandler2 = new ExpectationHandler(testMessages.Count, testIdentifier);

        var (app2, _) = await CreateMessagingAsync(new ChannelName(TopicOrQueueName), testHandler2, DateTime.UtcNow.AddHours(1));

        await using (app2)
        {
            await testHandler2.Completion;

            Assert.Equal(testMessages, testHandler2.MessagesReceives.ToList());
        }
    }

    [Fact]
    public async Task Should_publish_to_topic()
    {
        var testMessages = Enumerable.Range(0, 20).ToList();
        var testHandler1 = new ExpectationHandler(testMessages.Count, testIdentifier);
        var testHandler2 = new ExpectationHandler(testMessages.Count, testIdentifier);
        var topic = $"topic-{Guid.NewGuid()}";

        var channel = new ChannelName(TopicOrQueueName, ChannelType.Topic);

        var (app1, bus1) = await CreateMessagingAsync(channel, null, DateTime.UtcNow, options =>
        {
            options.Routing.Clear();
            options.Routing.AddFallback(channel);
        });

        var (app2, _) = await CreateMessagingAsync(channel, testHandler1, DateTime.UtcNow);
        var (app3, _) = await CreateMessagingAsync(channel, testHandler2, DateTime.UtcNow);

        await using (app1)
        await using (app2)
        await using (app3)
        {
            foreach (var message in testMessages)
            {
                var key = message.ToString(CultureInfo.InvariantCulture);

                await bus1.PublishAsync(new TestTypes.Message(testIdentifier, message), key);
            }

            await testHandler1.Completion;
            await testHandler2.Completion;

            Assert.Equal(testMessages, testHandler1.MessagesReceives.ToList());
            Assert.Equal(testMessages, testHandler2.MessagesReceives.ToList());
        }
    }
}
