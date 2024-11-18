// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Squidex.Messaging.RabbitMq;

internal sealed class RabbitMqSubscription : IMessageAck, IAsyncDisposable
{
    private readonly CancellationTokenSource stopToken = new CancellationTokenSource();
    private readonly string queueName;
    private readonly IChannel channel;
    private readonly ILogger log;
    private string consumerTag;

    private RabbitMqSubscription(string queueName, IChannel channel, ILogger log)
    {
        this.queueName = queueName;
        this.channel = channel;
        this.log = log;
    }

    public static async Task<RabbitMqSubscription> OpenAsync(
        string queueName, RabbitMqOwner
        factory,
        MessageTransportCallback callback,
        ILogger log,
        CancellationToken ct)
    {
        var rabbitMqChannel = await factory.CreateChannelAsync(ct);
        var rabbitMqSubscription = new RabbitMqSubscription(queueName, rabbitMqChannel, log);

        await rabbitMqSubscription.StartAsync(callback, ct);

        return rabbitMqSubscription;
    }

    internal async Task StartAsync(MessageTransportCallback callback,
        CancellationToken ct)
    {
        var eventConsumer = new AsyncEventingBasicConsumer(channel);

        eventConsumer.ReceivedAsync += async (_, @event) =>
        {
            try
            {
                var headers = new TransportHeaders();

                if (@event.BasicProperties.Headers != null)
                {
                    foreach (var (key, value) in @event.BasicProperties.Headers)
                    {
                        if (value is byte[] bytes)
                        {
                            headers[key] = Encoding.UTF8.GetString(bytes);
                        }
                        else if (value is string text)
                        {
                            headers[key] = text;
                        }
                    }
                }

                var transportMessage = new TransportMessage(@event.Body.ToArray(), @event.RoutingKey, headers);
                var transportResult = new TransportResult(transportMessage, @event.DeliveryTag);

                await callback(transportResult, this, stopToken.Token);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to handle message from queue {queue}.", queueName);
            }
        };

        consumerTag = await channel.BasicConsumeAsync(queueName, false, eventConsumer, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await stopToken.CancelAsync();
        try
        {
            await channel.BasicCancelAsync(consumerTag, cancellationToken: default);
        }
        finally
        {
            channel.Dispose();
        }
    }

    public async Task OnErrorAsync(TransportResult result,
        CancellationToken ct)
    {
        if (stopToken.IsCancellationRequested)
        {
            return;
        }

        if (result.Data is not ulong deliverTag)
        {
            log.LogWarning("Transport message has no RabbitMq delivery tag.");
            return;
        }

        try
        {
            await channel.BasicRejectAsync(deliverTag, false, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to acknowledge message from queue {queue}.", queueName);
        }
    }

    public async Task OnSuccessAsync(TransportResult result,
        CancellationToken ct)
    {
        if (stopToken.IsCancellationRequested)
        {
            return;
        }

        if (result.Data is not ulong deliverTag)
        {
            log.LogWarning("Transport message has no RabbitMq delivery tag.");
            return;
        }

        try
        {
            await channel.BasicAckAsync(deliverTag, false, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to reject message from queue {queue}.", queueName);
        }
    }
}
