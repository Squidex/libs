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
    private readonly IModel model;
    private readonly string queueName;
    private readonly string consumerTag;
    private readonly ILogger log;

    public RabbitMqSubscription(string queueName, RabbitMqOwner factory,
        MessageTransportCallback callback, ILogger log)
    {
        this.queueName = queueName;

        model = factory.Connection.CreateModel();

        var eventConsumer = new AsyncEventingBasicConsumer(model);

        eventConsumer.Received += async (_, @event) =>
        {
            var headers = new TransportHeaders();

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

            var transportMessage = new TransportMessage(@event.Body.ToArray(), @event.RoutingKey, headers);
            var transportResult = new TransportResult(transportMessage, @event.DeliveryTag);

            await callback(transportResult, this, stopToken.Token);
        };

        consumerTag = model.BasicConsume(queueName, false, eventConsumer);

        this.log = log;
    }

    public async ValueTask DisposeAsync()
    {
        await stopToken.CancelAsync();

        model.BasicCancel(consumerTag);
        model.Dispose();
    }

    public Task OnErrorAsync(TransportResult result,
        CancellationToken ct)
    {
        if (stopToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        if (result.Data is not ulong deliverTag)
        {
            log.LogWarning("Transport message has no RabbitMq delivery tag.");
            return Task.CompletedTask;
        }

        try
        {
            model.BasicReject(deliverTag, false);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to acknowledge message from queue {queue}.", queueName);
        }

        return Task.CompletedTask;
    }

    public Task OnSuccessAsync(TransportResult result,
        CancellationToken ct)
    {
        if (stopToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        if (result.Data is not ulong deliverTag)
        {
            log.LogWarning("Transport message has no RabbitMq delivery tag.");
            return Task.CompletedTask;
        }

        try
        {
            model.BasicAck(deliverTag, false);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to reject message from queue {queue}.", queueName);
        }

        return Task.CompletedTask;
    }
}
