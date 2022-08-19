// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Implementation
{
    public interface ISubscriptionManager
    {
        Task<IReadOnlyList<string>> GetSubscriptionsAsync(string topic,
            CancellationToken ct);

        Task<IAsyncDisposable> SubscribeAsync(string topic, string queue,
            CancellationToken ct);

        Task UnsubscribeAsync(string topic, string queue,
            CancellationToken ct);
    }
}
