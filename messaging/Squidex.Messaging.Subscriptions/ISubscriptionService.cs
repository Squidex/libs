// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions;

public interface ISubscriptionService
{
    Task<bool> HasSubscriptionsAsync(string key,
        CancellationToken ct = default);

    Task PublishAsync(string key, object message,
        CancellationToken ct = default);

    Task<IObservable<object>> SubscribeAsync(string key,
        CancellationToken ct = default);

    Task<IObservable<object>> SubscribeAsync(string key, ISubscription subscription,
        CancellationToken ct = default);
}
