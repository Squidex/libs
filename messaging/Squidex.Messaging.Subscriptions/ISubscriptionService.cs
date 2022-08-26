// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions
{
    public interface ISubscriptionService
    {
        bool HasSubscriptions { get; }

        Task PublishAsync(object message);

        IObservable<T> Subscribe<T>(ISubscription subscription);

        IObservable<T> Subscribe<T>();
    }
}
