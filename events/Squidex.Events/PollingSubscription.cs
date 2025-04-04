﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Events.Utils;

namespace Squidex.Events;

public sealed class PollingSubscription : IEventSubscription
{
    private readonly CompletionTimer timer;
#pragma warning disable IDE0052 // Remove unread private members
    private int eventsTotal;
#pragma warning restore IDE0052 // Remove unread private members

    public PollingSubscription(
        IEventStore eventStore,
        IEventSubscriber<StoredEvent> eventSubscriber,
        StreamFilter streamFilter,
        StreamPosition streamPosition,
        TimeSpan intervalMs)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(eventSubscriber);

        timer = new CompletionTimer(intervalMs, async ct =>
        {
            try
            {
                while (true)
                {
                    var eventsInAttempt = 0;
                    await foreach (var storedEvent in eventStore.QueryAllAsync(streamFilter, streamPosition, ct: ct))
                    {
                        await eventSubscriber.OnNextAsync(this, storedEvent);

                        streamPosition = storedEvent.EventPosition;
                        eventsInAttempt++;
                        eventsTotal++;
                    }

                    if (eventsInAttempt == 0)
                    {
                        break;
                    }

                    await Task.Delay(100, ct);
                }
            }
            catch (Exception ex)
            {
                await eventSubscriber.OnErrorAsync(this, ex);
            }
        });
    }

    public ValueTask CompleteAsync()
    {
        return new ValueTask(timer.StopAsync());
    }

    public void Dispose()
    {
        timer.StopAsync().Forget();
    }

    public void WakeUp()
    {
        timer.SkipCurrentDelay();
    }
}
