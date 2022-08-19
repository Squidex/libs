// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Implementation
{
    public interface ISubscriptionStore
    {
        Task<IReadOnlyList<string>> GetSubscriptionsAsync(string topic, DateTime now,
            CancellationToken ct);

        Task SubscribeAsync(string topic, string queue, DateTime now, TimeSpan expiresAfter,
            CancellationToken ct);

        Task UnsubscribeAsync(string topic, string queue,
            CancellationToken ct);

        Task UpdateAliveAsync(string[] queues, DateTime now,
            CancellationToken ct);

        Task CleanupAsync(DateTime now,
            CancellationToken ct);
    }
}
