// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Squidex.Messaging.Internal;

namespace Squidex.Messaging.Kafka;

public sealed class KafkaTransport : IMessagingTransport
{
    private readonly KafkaOwner owner;
    private readonly ILogger<KafkaTransport> log;
    private IProducer<string, byte[]>? producer;

    public KafkaTransport(KafkaOwner owner,
        ILogger<KafkaTransport> log)
    {
        this.owner = owner;

        this.log = log;
    }

    public Task InitializeAsync(
        CancellationToken ct)
    {
        producer =
            new DependentProducerBuilder<string, byte[]>(owner.Handle)
                .Build();

        return Task.CompletedTask;
    }

    public Task ReleaseAsync(
        CancellationToken ct)
    {
        if (producer != null)
        {
            producer.Flush(ct);
            producer.Dispose();
        }

        return Task.CompletedTask;
    }

    public Task<IAsyncDisposable?> CreateChannelAsync(ChannelName channel, string instanceName, bool consume, ProducerOptions producerOptions,
        CancellationToken ct)
    {
        if (channel.Type == ChannelType.Topic)
        {
            ThrowHelper.InvalidOperationException("Topics are not supported.");
        }

        return Task.FromResult<IAsyncDisposable?>(null);
    }

    public async Task ProduceAsync(ChannelName channel, string instanceName, TransportMessage transportMessage,
        CancellationToken ct)
    {
        if (producer == null)
        {
            ThrowHelper.InvalidOperationException("Transport has not been initialized yet.");
            return;
        }

        if (channel.Type == ChannelType.Topic)
        {
            ThrowHelper.InvalidOperationException("Topics are not supported.");
            return;
        }

        var message = new Message<string, byte[]>
        {
            Value = transportMessage.Data
        };

        if (transportMessage.Headers.Count > 0)
        {
            message.Headers = [];

            foreach (var (key, value) in transportMessage.Headers)
            {
                message.Headers.Add(key, Encoding.UTF8.GetBytes(value));
            }
        }

        if (string.IsNullOrWhiteSpace(transportMessage.Key))
        {
            message.Key = Guid.NewGuid().ToString();
        }
        else
        {
            message.Key = transportMessage.Key;
        }

        try
        {
            await producer.ProduceAsync(channel.Name, message, ct);
        }
        catch (ProduceException<string, byte[]> ex) when (ex.Error.Code == ErrorCode.Local_QueueFull)
        {
            while (true)
            {
                try
                {
                    producer.Poll(Timeout.InfiniteTimeSpan);

                    await producer.ProduceAsync(channel.Name, message, ct);

                    return;
                }
                catch (ProduceException<string, byte[]> ex2) when (ex2.Error.Code == ErrorCode.Local_QueueFull)
                {
                    await Task.Delay(100, ct);
                }
            }
        }
    }

    public Task<IAsyncDisposable> SubscribeAsync(ChannelName channel, string instanceName, MessageTransportCallback callback,
        CancellationToken ct)
    {
        return Task.FromResult<IAsyncDisposable>(SubscribeCore(channel, callback));
    }

    private KafkaSubscription SubscribeCore(ChannelName channel, MessageTransportCallback callback)
    {
        if (producer == null)
        {
            ThrowHelper.InvalidOperationException("Transport has not been initialized yet.");
            return default!;
        }

        if (channel.Type == ChannelType.Topic)
        {
            ThrowHelper.InvalidOperationException("Topics are not supported.");
            return default!;
        }

        return new KafkaSubscription(channel.Name, callback, owner, log);
    }
}
