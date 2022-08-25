// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging
{
    public interface IMessagingSubscriptionStore
    {
        Task<IReadOnlyList<(string Key, SerializedObject Value)>> GetSubscriptionsAsync(string group, DateTime now,
            CancellationToken ct);

        Task SubscribeAsync(string group, string key, SerializedObject value, DateTime now, TimeSpan expiresAfter,
            CancellationToken ct);

        Task UnsubscribeAsync(string group, string key,
            CancellationToken ct);

        Task UpdateAliveAsync(string group, string[] keys, DateTime now,
            CancellationToken ct);

        Task CleanupAsync(DateTime now,
            CancellationToken ct);
    }
}
