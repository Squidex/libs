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
        Task PublishAsync<T>(T message) where T : notnull;

        ILocalSubscription<T> Subscribe<T, TSubscription>(TSubscription subscription) where TSubscription : ISubscription, new();

        ILocalSubscription<T> Subscribe<T>();
    }
}
