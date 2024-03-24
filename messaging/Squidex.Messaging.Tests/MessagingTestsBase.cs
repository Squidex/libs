// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using FakeItEasy;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Internal;
using Xunit;

namespace Squidex.Messaging;

[Trait("Category", "Dependencies")]
public abstract class MessagingTestsBase
{
    private readonly Guid testIdentifier = Guid.NewGuid();
    private DateTime now = DateTime.UtcNow;

    protected virtual bool CanHandleAndSimulateTimeout { get; } = true;

    protected virtual string TopicOrQueueName { get; } = $"channel_topic_{Guid.NewGuid()}";

    [Fact]
    public async Task Should_throw_exception_if_no_route_found()
    {
        var consumer = new DelegatingHandler<TestMessage>(message =>
        {
            return Task.CompletedTask;
        });

        await using var app = await CreateMessagingAsync(new ChannelName(TopicOrQueueName), consumer, options =>
        {
            options.Routing.Clear();
        });

        await Assert.ThrowsAnyAsync<Exception>(() => app.Sut.PublishAsync(213));
    }

    [Fact]
    public async Task Should_not_throw_exception_if_no_channel_found()
    {
        var consumer = new DelegatingHandler<TestMessage>(message =>
        {
            return Task.CompletedTask;
        });

        await using var app = await CreateMessagingAsync(new ChannelName(TopicOrQueueName), consumer, options =>
        {
            options.Routing.Clear();
        });

        var message = new TestMessage(Guid.NewGuid(), 10);

        await app.Sut.PublishToChannelAsync(message, new ChannelName("invalid"));
    }

    [Fact]
    public async Task Should_consume_base_classes()
    {
        var testMessages = Enumerable.Range(0, 20).ToList();
        var testHandler = new ExpectationHandler(testMessages.Count, testIdentifier);

        await using var app = await CreateMessagingAsync(new ChannelName(TopicOrQueueName), testHandler);

        foreach (var message in testMessages)
        {
            var messageKey = message.ToString(CultureInfo.InvariantCulture);
            var messageData = new TestMessage(testIdentifier, message) as BaseMessage;

            await app.Sut.PublishAsync(messageData, messageKey);
        }

        await testHandler.Completion;

        Assert.Equal(testMessages, testHandler.MessagesReceives.ToList());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(20)]
    public async Task Should_consume_messages(int numConsumers)
    {
        var testMessages = Enumerable.Range(0, 20).ToList();
        var testHandler = new ExpectationHandler(testMessages.Count, testIdentifier);

        var apps = new List<Provider<IMessageBus>>();

        for (var i = 0; i < numConsumers; i++)
        {
            apps.Add(await CreateMessagingAsync(new ChannelName(TopicOrQueueName), testHandler));
        }

        try
        {
            var bus = apps[0].Sut;

            foreach (var message in testMessages)
            {
                var messageKey = message.ToString(CultureInfo.InvariantCulture);
                var messageData = new TestMessage(testIdentifier, message);

                await bus.PublishAsync(messageData, messageKey);
            }

            await testHandler.Completion;

            Assert.Equal(testMessages, testHandler.MessagesReceives.ToList());
        }
        finally
        {
            foreach (var app in apps)
            {
                await app.DisposeAsync();
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

        var consumer1 = new DelegatingHandler<TestMessage>(message =>
        {
            return Task.Delay(TimeSpan.FromDays(30));
        });

        await using (var app1 = await CreateMessagingAsync(new ChannelName(TopicOrQueueName), consumer1))
        {
            foreach (var message in testMessages)
            {
                var messageKey = message.ToString(CultureInfo.InvariantCulture);
                var messageData = new TestMessage(testIdentifier, message);

                await app1.Sut.PublishAsync(messageData, messageKey);
            }
        }

        var testHandler2 = new ExpectationHandler(testMessages.Count, testIdentifier);

        now = now.AddHours(1);

        await using (var app2 = await CreateMessagingAsync(new ChannelName(TopicOrQueueName), testHandler2))
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

        await using var app1 = await CreateMessagingAsync(channel, null, options =>
        {
            options.Routing.Clear();
            options.Routing.AddFallback(channel);
        });

        await using var app2 = await CreateMessagingAsync(channel, testHandler1);
        await using var app3 = await CreateMessagingAsync(channel, testHandler2);

        foreach (var message in testMessages)
        {
            var messageKey = message.ToString(CultureInfo.InvariantCulture);
            var messageData = new TestMessage(testIdentifier, message);

            await app1.Sut.PublishAsync(messageData, messageKey);
        }

        await testHandler1.Completion;
        await testHandler2.Completion;

        Assert.Equal(testMessages, testHandler1.MessagesReceives.ToList());
        Assert.Equal(testMessages, testHandler2.MessagesReceives.ToList());
    }

    private Task<Provider<IMessageBus>> CreateMessagingAsync(ChannelName channel, IMessageHandler? handler,
        Action<MessagingOptions>? configure = null)
    {
        var clock = A.Fake<TimeProvider>();

        A.CallTo(() => clock.GetUtcNow())
            .Returns(new DateTimeOffset(now, default));

        var serviceProvider =
            new ServiceCollection()
                .AddLogging(options =>
                {
                    options.AddDebug();
                    options.AddConsole();
                })
                .AddMessaging()
                    .Configure<MessagingOptions>(options =>
                    {
                        options.Routing.AddFallback(channel);
                    })
                    .Configure(configure)
                    .AddChannel(channel, handler != null, options =>
                    {
                        options.Expires = TimeSpan.FromDays(1);
                    })
                    .AddSubscriptions()
                    .AddOverride(Configure)
                    .AddHandler(handler)
                    .Services
                .AddSingleton(clock)
                .AddSingleton<IInstanceNameProvider, RandomInstanceNameProvider>()
                .BuildServiceProvider();

        return serviceProvider.CreateAsync<IMessageBus>();
    }

    protected abstract void Configure(MessagingBuilder builder);
}
