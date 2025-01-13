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
    private readonly CompletionTimer timer;

    public PollingSubscription(
        IEventStore eventStore,
        IEventSubscriber<StoredEvent> eventSubscriber,
        StreamFilter streamFilter,
        TimeSpan intervalMs,
        string? position)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(eventSubscriber);

        timer = new CompletionTimer(intervalMs, async ct =>
        {
            try
            {
                await foreach (var storedEvent in eventStore.QueryAllAsync(streamFilter, position, ct: ct))
                {
                    await eventSubscriber.OnNextAsync(this, storedEvent);

                    position = storedEvent.EventPosition;
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
