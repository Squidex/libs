// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions;

public interface ISubscriptionService
{
    Task<bool> HasSubscriptionsAsync<T>(
        CancellationToken ct = default) where T : notnull;

    Task PublishAsync<T>(T message,
        CancellationToken ct = default) where T : notnull;

    Task PublishWrapperAsync<T>(IPayloadWrapper<T> message,
        CancellationToken ct = default) where T : notnull;

    Task<IObservable<T>> SubscribeAsync<T>(
        CancellationToken ct = default) where T : notnull;

    Task<IObservable<T>> SubscribeAsync<T>(ISubscription<T> subscription,
        CancellationToken ct = default) where T : notnull;
}
