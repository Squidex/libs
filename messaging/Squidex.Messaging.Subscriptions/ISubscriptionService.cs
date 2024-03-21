// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions;

public interface ISubscriptionService
{
    Task<bool> HasSubscriptionsAsync<T>(string key,
        CancellationToken ct = default) where T : notnull;

    Task PublishAsync<T>(string key, T message,
        CancellationToken ct = default) where T : notnull;

    Task PublishWrapperAsync<T>(string key, IPayloadWrapper<T> message,
        CancellationToken ct = default) where T : notnull;

    Task<IObservable<T>> SubscribeAsync<T>(string key,
        CancellationToken ct = default) where T : notnull;

    Task<IObservable<T>> SubscribeAsync<T>(string key, ISubscription<T> subscription,
        CancellationToken ct = default) where T : notnull;
}
