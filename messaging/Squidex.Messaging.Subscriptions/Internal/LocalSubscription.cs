// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.Internal;
using Squidex.Messaging.Subscriptions.Implementation;

namespace Squidex.Messaging.Subscriptions.Internal;

internal sealed class LocalSubscription : IObservable<object>, IDisposable
{
    private readonly Action unsubscribe;
    private IObserver<object>? currentObserver;

    public string Id { get; } = Guid.NewGuid().ToString();

    public LocalSubscription(SubscriptionService subscriptions, string key)
    {
        unsubscribe = () =>
        {
            subscriptions.UnsubscribeAsync(Id, key).Forget();
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

    public IDisposable Subscribe(IObserver<object> observer)
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
        if (value != null)
        {
            currentObserver?.OnNext(value);
        }
    }
}
