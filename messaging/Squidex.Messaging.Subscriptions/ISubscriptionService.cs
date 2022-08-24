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

        Task PublishAsync<T>(T message) where T : notnull;

        Task PublishWrapperAsync<T>(IPayloadWrapper<T> wrapper) where T : notnull;

        IObservable<T> Subscribe<T, TSubscription>(TSubscription subscription) where TSubscription : ISubscription, new();

        IObservable<T> Subscribe<T>();
    }
}
