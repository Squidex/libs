// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Logging;
using Squidex.Messaging.Internal;

namespace Squidex.Messaging.GoogleCloud;

internal sealed class GooglePubSubSubscription : IMessageAck, IAsyncDisposable
{
    private readonly SubscriberClient subscriberClient;

    public GooglePubSubSubscription(SubscriberClient subscriberClient, MessageTransportCallback callback,
        ILogger log)
    {
        this.subscriberClient = subscriberClient;

        SubscribeCoreAsync(callback, log).Forget();
    }

    private async Task SubscribeCoreAsync(MessageTransportCallback callback, ILogger log)
    {
        try
        {
            await subscriberClient!.StartAsync(async (pubSubMessage, ct) =>
            {
                var headers = new TransportHeaders();

                foreach (var (key, value) in pubSubMessage.Attributes)
                {
                    headers.Set(key, value);
                }

                var transportMessage = new TransportMessage(pubSubMessage.Data.ToArray(), pubSubMessage.OrderingKey, headers);
                var transportResult = new TransportResult(transportMessage, null);

                await callback(transportResult, this, ct);

                return SubscriberClient.Reply.Ack;
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Failed to consume message from subscription '{subscription}'.", subscriberClient.SubscriptionName.SubscriptionId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await subscriberClient.StopAsync(default(CancellationToken));
    }

    public Task OnErrorAsync(TransportResult result,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task OnSuccessAsync(TransportResult result,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
