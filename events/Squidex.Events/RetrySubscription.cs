// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Events.Utils;

namespace Squidex.Events;

public sealed class RetrySubscription<T> : IEventSubscription, IEventSubscriber<T>
{
    private readonly RetryWindow retryWindow = new RetryWindow(TimeSpan.FromMinutes(5), 5);
    private readonly CancellationTokenSource disposeToken = new CancellationTokenSource();
    private readonly IEventSubscriber<T> eventSubscriber;
    private readonly EventSubscriptionSource<T> eventSource;
    private IEventSubscription? currentSubscription;

    public int ReconnectWaitMs { get; set; } = 5000;

    public bool IsSubscribed => currentSubscription != null;

    public RetrySubscription(IEventSubscriber<T> eventSubscriber,
        EventSubscriptionSource<T> eventSource)
    {
        ArgumentNullException.ThrowIfNull(eventSubscriber);
        ArgumentNullException.ThrowIfNull(eventSource);

        this.eventSubscriber = eventSubscriber;
        this.eventSource = eventSource;

        Subscribe();
    }

    public void Dispose()
    {
        lock (retryWindow)
        {
            disposeToken.Cancel();
        }

        Unsubscribe();
    }

    private void Subscribe()
    {
        lock (retryWindow)
        {
            // Never resurrect the subscription after it has been disposed during the reconnect delay.
            if (currentSubscription != null || disposeToken.IsCancellationRequested)
            {
                return;
            }

            currentSubscription = eventSource(this);
        }
    }

    private void Unsubscribe()
    {
        lock (retryWindow)
        {
            if (currentSubscription == null)
            {
                return;
            }

            currentSubscription.Dispose();
            currentSubscription = null;
        }
    }

    public void WakeUp()
    {
        currentSubscription?.WakeUp();
    }

    public ValueTask CompleteAsync()
    {
        return currentSubscription?.CompleteAsync() ?? default;
    }

    async ValueTask IEventSubscriber<T>.OnNextAsync(IEventSubscription subscription, T @event)
    {
        if (!ReferenceEquals(subscription, currentSubscription))
        {
            return;
        }

        await eventSubscriber.OnNextAsync(this, @event);
    }

    async ValueTask IEventSubscriber<T>.OnErrorAsync(IEventSubscription subscription, Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return;
        }

        if (!ReferenceEquals(subscription, currentSubscription))
        {
            return;
        }

        Unsubscribe();

        if (!retryWindow.CanRetryAfterFailure())
        {
            await eventSubscriber.OnErrorAsync(this, exception);
            return;
        }

        try
        {
            await Task.Delay(ReconnectWaitMs, disposeToken.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Subscribe();
    }
}
