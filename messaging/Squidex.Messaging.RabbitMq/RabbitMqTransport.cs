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

public sealed class RabbitMqTransport : IMessagingTransport
{
    private readonly RabbitMqOwner owner;
    private readonly ILogger<RabbitMqTransport> log;
    private readonly HashSet<string> createdQueues = [];
    private IModel? model;

    public RabbitMqTransport(RabbitMqOwner owner,
        ILogger<RabbitMqTransport> log)
    {
        this.owner = owner;

        this.log = log;
    }

    public Task InitializeAsync(
        CancellationToken ct)
    {
        if (model != null)
        {
            return Task.CompletedTask;
        }

        model = owner.Connection.CreateModel();

        return Task.CompletedTask;
    }

    public Task ReleaseAsync(
        CancellationToken ct)
    {
        if (model == null)
        {
            return Task.CompletedTask;
        }

        model.Dispose();
        model = null;

        return Task.CompletedTask;
    }

    public Task<IAsyncDisposable?> CreateChannelAsync(ChannelName channel, string instanceName, bool consume, ProducerOptions producerOptions,
        CancellationToken ct)
    {
        if (model == null)
        {
            ThrowHelper.InvalidOperationException("Transport not initialized yet.");
            return Task.FromResult<IAsyncDisposable?>(null);
        }

        lock (model)
        {
            if (channel.Type == ChannelType.Queue)
            {
                model.QueueDeclare(channel.Name, true, false, false, null);
            }
            else
            {
                model.ExchangeDeclare(channel.Name, "fanout", true, false, null);
            }
        }

        return Task.FromResult<IAsyncDisposable?>(null);
    }

    public Task ProduceAsync(ChannelName channel, string instanceName, TransportMessage transportMessage,
        CancellationToken ct)
    {
        if (model == null)
        {
            ThrowHelper.InvalidOperationException("Transport not initialized yet.");
            return Task.CompletedTask;
        }

        lock (model)
        {
            var properties = model.CreateBasicProperties();

            if (transportMessage.Headers.Count > 0)
            {
                properties.Headers = new Dictionary<string, object>();

                foreach (var (key, value) in transportMessage.Headers)
                {
                    properties.Headers[key] = value;
                }
            }

            if (channel.Type == ChannelType.Queue)
            {
                model.BasicPublish(string.Empty, channel.Name, properties, transportMessage.Data);
            }
            else
            {
                model.BasicPublish(channel.Name, "*", properties, transportMessage.Data);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IAsyncDisposable> SubscribeAsync(ChannelName channel, string instanceName, MessageTransportCallback callback,
        CancellationToken ct)
    {
        return Task.FromResult<IAsyncDisposable>(SubscribeCore(channel, instanceName, callback));
    }

    private RabbitMqSubscription SubscribeCore(ChannelName channel, string instanceName, MessageTransportCallback callback)
    {
        if (model == null)
        {
            ThrowHelper.InvalidOperationException("Transport not initialized yet.");
            return null!;
        }

        var queueName = channel.Name;

        if (channel.Type == ChannelType.Topic)
        {
            queueName = CreateTemporaryQueue(channel, instanceName);
        }

        return new RabbitMqSubscription(queueName, owner, callback, log);
    }

    private string CreateTemporaryQueue(ChannelName channel, string instanceName)
    {
        var queueName = $"{channel.Name}_{instanceName}";

        if (!createdQueues.Add(queueName))
        {
            return queueName;
        }

        lock (model!)
        {
            // Create a queue that only lives as long as the client is connected.
            model.QueueDeclare(queueName);

            // Bind the queue to the exchange.
            model.QueueBind(queueName, channel.Name, "*");
        }

        return queueName;
    }
}
