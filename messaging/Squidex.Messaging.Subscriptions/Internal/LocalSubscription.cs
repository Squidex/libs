// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions.Internal;

internal sealed class LocalSubscription<T> : IObservable<T>, IUntypedLocalSubscription, IDisposable
{
    private readonly SubscriptionService subscriptions;
    private readonly ISubscription subscription;
    private IObserver<T>? currentObserver;

    public Guid Id { get; } = Guid.NewGuid();

    public LocalSubscription(SubscriptionService subscriptions, ISubscription subscription)
    {
        this.subscriptions = subscriptions;
        this.subscription = subscription;
    }

    private void SubscribeCore(IObserver<T> observer)
    {
        if (currentObserver != null)
        {
            throw new InvalidOperationException("Can only have one observer.");
        }

        subscriptions.SubscribeCore(Id, this, subscription);

        currentObserver = observer;
    }

    void IDisposable.Dispose()
    {
        if (currentObserver == null)
        {
            return;
        }

        subscriptions.UnsubscribeCore(Id);

        currentObserver = null;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        SubscribeCore(observer);

        return this;
    }

    public void OnError(Exception error)
    {
        if (error != null)
        {
            currentObserver?.OnError(error);
        }
    }

    public void OnNext(object? value)
    {
        if (value is T typed)
        {
            currentObserver?.OnNext(typed);
        }
    }
}
