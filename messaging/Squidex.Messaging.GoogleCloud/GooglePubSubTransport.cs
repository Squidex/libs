// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Messaging.Internal;
using GooglePushConfig = Google.Cloud.PubSub.V1.PushConfig;

namespace Squidex.Messaging.GoogleCloud;

public sealed class GooglePubSubTransport : IMessagingTransport
{
    private readonly Dictionary<string, Task<PublisherClient>> publishers = new Dictionary<string, Task<PublisherClient>>();
    private readonly GooglePubSubTransportOptions options;
    private readonly GooglePushConfig pushConfig = new GooglePushConfig();
    private readonly HashSet<string> createdSubcriptions = new HashSet<string>();
    private readonly HashSet<string> createdTopics = new HashSet<string>();
    private readonly ILogger<GooglePubSubTransport> log;

    public GooglePubSubTransport(IOptions<GooglePubSubTransportOptions> options,
        ILogger<GooglePubSubTransport> log)
    {
        this.options = options.Value;

        this.log = log;
    }

    public Task InitializeAsync(
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public async Task ReleaseAsync(
        CancellationToken ct)
    {
        foreach (var (_, publisherClientTask) in publishers)
        {
            var publisherClient = await publisherClientTask;

            await publisherClient.ShutdownAsync(ct);
        }

        publishers.Clear();
    }

    public Task<IAsyncDisposable?> CreateChannelAsync(ChannelName channel, string instanceName, bool consume, ProducerOptions producerOptions,
        CancellationToken ct)
    {
        if (publishers.ContainsKey(channel.Name))
        {
            return Task.FromResult<IAsyncDisposable?>(null);
        }

        publishers[channel.Name] = new Func<Task<PublisherClient>>(async () =>
        {
            var topicName = new TopicName(options.ProjectId, $"{options.Prefix}{channel.Name}");

            await CreateTopicAsync(topicName, default);

            if (channel.Type == ChannelType.Queue)
            {
                await CreateSubscriptionAsync(producerOptions, topicName, default);
            }

            return await PublisherClient.CreateAsync(topicName);
        })();

        return Task.FromResult<IAsyncDisposable?>(null);
    }

    private async Task CreateTopicAsync(TopicName name,
        CancellationToken ct)
    {
        var topicName = name;
        var topicApi = await PublisherServiceApiClient.CreateAsync(ct);

        if (await HasTopicAsync(topicApi, topicName, ct))
        {
            return;
        }

        try
        {
            await topicApi.CreateTopicAsync(topicName, ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            // This exception is expected.
        }
    }

    private async Task CreateSubscriptionAsync(ProducerOptions producerOptions, TopicName topicName,
        CancellationToken ct)
    {
        var subscriptionName = new SubscriptionName(options.ProjectId, topicName.TopicId);
        var subscriptionApi = await SubscriberServiceApiClient.CreateAsync(ct);

        // The subscription is created last, so we jump out early.
        if (await HasSubscriptionAsync(subscriptionApi, subscriptionName, ct))
        {
            return;
        }

        var timeoutInSec = (int)Math.Min(producerOptions.Timeout.TotalSeconds, 600);
        try
        {
            await subscriptionApi.CreateSubscriptionAsync(subscriptionName, topicName, pushConfig, timeoutInSec, ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            // This exception is expected.
        }
    }

    private async Task<bool> HasTopicAsync(PublisherServiceApiClient client, TopicName name,
        CancellationToken ct)
    {
        if (!createdTopics.Add(name.ToString()))
        {
            return true;
        }

        try
        {
            await client.GetTopicAsync(name, ct);
            return true;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task<bool> HasSubscriptionAsync(SubscriberServiceApiClient client, SubscriptionName name,
        CancellationToken ct)
    {
        if (!createdSubcriptions.Add(name.ToString()))
        {
            return true;
        }

        try
        {
            await client.GetSubscriptionAsync(name, ct);
            return true;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task ProduceAsync(ChannelName channel, string instanceName, TransportMessage transportMessage,
        CancellationToken ct)
    {
        var publisherClient = await GetPublisherAsync(channel.Name);

        var pubSubMessage = new PubsubMessage
        {
            Data = ByteString.CopyFrom(transportMessage.Data)
        };

        foreach (var (key, value) in transportMessage.Headers)
        {
            pubSubMessage.Attributes[key] = value;
        }

        await publisherClient.PublishAsync(pubSubMessage);
    }

    public async Task<IAsyncDisposable> SubscribeAsync(ChannelName channel, string instanceName, MessageTransportCallback callback,
        CancellationToken ct)
    {
        var publisherClient = await GetPublisherAsync(channel.Name);

        var subscriptionName = new SubscriptionName(options.ProjectId, publisherClient.TopicName.TopicId);

        if (channel.Type == ChannelType.Topic)
        {
            subscriptionName = await CreateTemporarySubscriptionAsync(publisherClient, subscriptionName, instanceName, ct);
        }

        var subscriberClient = await SubscriberClient.CreateAsync(subscriptionName);

        return new GooglePubSubSubscription(subscriberClient, callback, log);
    }

    private async Task<SubscriptionName> CreateTemporarySubscriptionAsync(PublisherClient publisherClient, SubscriptionName subscriptionName, string instanceName,
        CancellationToken ct)
    {
        var subscriptionApi = await SubscriberServiceApiClient.CreateAsync(ct);

        subscriptionName = new SubscriptionName(subscriptionName.ProjectId, $"{subscriptionName.SubscriptionId}_{instanceName}");

        if (await HasSubscriptionAsync(subscriptionApi, subscriptionName, ct))
        {
            return subscriptionName;
        }

        var request = new Subscription
        {
            SubscriptionName = subscriptionName,
            ExpirationPolicy = new ExpirationPolicy
            {
                Ttl = Duration.FromTimeSpan(TimeSpan.FromDays(2))
            },
            TopicAsTopicName = publisherClient.TopicName,
        };

        await subscriptionApi.CreateSubscriptionAsync(request, ct);

        return subscriptionName;
    }

    private async Task<PublisherClient> GetPublisherAsync(string channelName)
    {
        if (!publishers.TryGetValue(channelName, out var publisherClientTask))
        {
            ThrowHelper.InvalidOperationException("Channel has not been initialized yet.");
            return default!;
        }

        return await publisherClientTask;
    }
}
