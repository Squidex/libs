// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.Internal;
using Squidex.Messaging.Subscriptions.Implementation;

namespace Squidex.Messaging.Subscriptions.Internal;

internal sealed class LocalSubscription<T> : IObservable<T>, IUntypedLocalSubscription, IDisposable
{
    private readonly SubscriptionService subscriptions;
    private IObserver<T>? currentObserver;

    public string Id { get; } = Guid.NewGuid().ToString();

    public LocalSubscription(SubscriptionService subscriptions)
    {
        this.subscriptions = subscriptions;
    }

    void IDisposable.Dispose()
    {
        if (currentObserver == null)
        {
            return;
        }

        subscriptions.UnsubscribeAsync<T>(Id).Forget();
        currentObserver = null;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        currentObserver = observer;
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
