// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using EventStore.Client;
using Grpc.Core;

namespace Squidex.Events.GetEventStore;

internal sealed class GetEventStoreSubscription : IEventSubscription
{
    private readonly CancellationTokenSource cts = new CancellationTokenSource();

    public GetEventStoreSubscription(
        IEventSubscriber<StoredEvent> eventSubscriber,
        EventStoreClient client,
        EventStoreProjectionClient projectionClient,
        string? prefix,
        StreamFilter filter,
        StreamPosition position)
    {
        var ct = cts.Token;

#pragma warning disable MA0134 // Observe result of async calls
        Task.Run(async () =>
        {
            var streamName = await projectionClient.CreateProjectionAsync(filter, false, default);

            var start = FromStream.Start;
            if (position.ReadFromEnd)
            {
                start = FromStream.End;
            }
            else if (!string.IsNullOrWhiteSpace(position))
            {
                start = FromStream.After(position.ToPosition(true));
            }

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                await using var subscription = client.SubscribeToStream(streamName, start, true, cancellationToken: ct);
                try
                {
                    await foreach (var message in subscription.Messages.OfType<StreamMessage.Event>().WithCancellation(ct))
                    {
                        var storedEvent = Formatter.Read(message.ResolvedEvent, prefix);

                        await eventSubscriber.OnNextAsync(this, storedEvent);

                        // In some cases we have to resubscribe again, therefore we need the position.
                        start = FromStream.After(message.ResolvedEvent.OriginalEventNumber);
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Aborted)
                {
                    // Consumer too slow.
                }
                catch (Exception ex)
                {
                    var inner = new InvalidOperationException($"Subscription closed.", ex);

                    await eventSubscriber.OnErrorAsync(this, ex);
                }
            }
        }, ct);
#pragma warning restore MA0134 // Observe result of async calls
    }

    public void Dispose()
    {
        cts.Cancel();
    }

    public ValueTask CompleteAsync()
    {
        return default;
    }

    public void WakeUp()
    {
    }
}
