// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Events.Utils;

namespace Squidex.Events;

public sealed class PollingSubscription : IEventSubscription
{
    private const int PageSize = 1000;
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

                    // Query in pages. Without a limit the store would be asked for the whole stream in
                    // one go, which keeps an unbounded number of events in memory while catching up.
                    await foreach (var storedEvent in eventStore.QueryAllAsync(streamFilter, streamPosition, PageSize, ct))
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

                    if (eventsInAttempt < PageSize)
                    {
                        // Close to the end of the stream, so do not query the store in a tight loop.
                        // A full page means we are still catching up and continue immediately.
                        await Task.Delay(100, ct);
                    }
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
