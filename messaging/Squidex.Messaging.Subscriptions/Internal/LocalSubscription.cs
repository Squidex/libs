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
    private readonly Action unsubscribe;
    private IObserver<T>? currentObserver;

    public string Id { get; } = Guid.NewGuid().ToString();

    public LocalSubscription(SubscriptionService subscriptions, string key)
    {
        unsubscribe = () =>
        {
            subscriptions.UnsubscribeAsync<T>(Id, key).Forget();
        };
    }

    void IDisposable.Dispose()
    {
        if (currentObserver == null)
        {
            return;
        }

        try
        {
            unsubscribe();
        }
        finally
        {
            currentObserver = null;
        }
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
