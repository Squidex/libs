// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Squidex.Messaging.Internal;

namespace Squidex.Messaging.RabbitMq;

public sealed class RabbitMqTransport(
    RabbitMqOwner owner,
    ILogger<RabbitMqTransport> log)
    : IMessagingTransport
{
    private readonly HashSet<string> createdQueues = [];
    private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
    private IChannel? channelModel;

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        if (channelModel != null)
        {
            return;
        }

        channelModel = await owner.CreateChannelAsync(ct);
    }

    public Task ReleaseAsync(
        CancellationToken ct)
    {
        if (channelModel == null)
        {
            return Task.CompletedTask;
        }

        channelModel.Dispose();
        channelModel = null;

        return Task.CompletedTask;
    }

    public async Task<IAsyncDisposable?> CreateChannelAsync(ChannelName channel, string instanceName, bool consume, ProducerOptions producerOptions,
        CancellationToken ct)
    {
        if (channelModel == null)
        {
            ThrowHelper.InvalidOperationException("Transport not initialized yet.");
            return null;
        }

        await semaphoreSlim.WaitAsync(ct);
        try
        {
            if (channel.Type == ChannelType.Queue)
            {
                await channelModel.QueueDeclareAsync(channel.Name, true, false, false, null, cancellationToken: ct);
            }
            else
            {
                await channelModel.ExchangeDeclareAsync(channel.Name, "fanout", true, false, null, cancellationToken: ct);
            }
        }
        finally
        {
            semaphoreSlim.Release();
        }

        return null;
    }

    public async Task ProduceAsync(ChannelName channel, string instanceName, TransportMessage transportMessage,
        CancellationToken ct)
    {
        if (channelModel == null)
        {
            ThrowHelper.InvalidOperationException("Transport not initialized yet.");
            return;
        }

        await semaphoreSlim.WaitAsync(ct);
        try
        {
            var properties = new BasicProperties();

            if (transportMessage.Headers.Count > 0)
            {
                properties.Headers = new Dictionary<string, object?>();

                foreach (var (key, value) in transportMessage.Headers)
                {
                    properties.Headers[key] = value;
                }
            }

            if (channel.Type == ChannelType.Queue)
            {
                await channelModel.BasicPublishAsync(string.Empty, channel.Name, false, properties, transportMessage.Data, ct);
            }
            else
            {
                await channelModel.BasicPublishAsync(channel.Name, "*", false, properties, transportMessage.Data, ct);
            }
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    public async Task<IAsyncDisposable> SubscribeAsync(ChannelName channel, string instanceName, MessageTransportCallback callback,
        CancellationToken ct)
    {
        if (channelModel == null)
        {
            ThrowHelper.InvalidOperationException("Transport not initialized yet.");
            return null!;
        }

        var queueName = channel.Name;

        if (channel.Type == ChannelType.Topic)
        {
            queueName = await CreateTemporaryQueueAsync(channel, instanceName, ct);
        }

        return await RabbitMqSubscription.OpenAsync(queueName, owner, callback, log, ct);
    }

    private async Task<string> CreateTemporaryQueueAsync(ChannelName channel, string instanceName,
        CancellationToken ct)
    {
        var queueName = $"{channel.Name}_{instanceName}";

        if (!createdQueues.Add(queueName))
        {
            return queueName;
        }

        await semaphoreSlim.WaitAsync(ct);
        try
        {
            // Create a queue that only lives as long as the client is connected.
            await channelModel!.QueueDeclareAsync(queueName, cancellationToken: ct);
            await channelModel!.QueueBindAsync(queueName, channel.Name, "*", cancellationToken: ct);
        }
        finally
        {
            semaphoreSlim.Release();
        }

        return queueName;
    }
}
