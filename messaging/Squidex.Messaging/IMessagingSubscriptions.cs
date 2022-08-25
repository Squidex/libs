// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Implementation
{
    public interface IMessagingSubscriptions
    {
        Task<IReadOnlyDictionary<string, T>> GetSubscriptionsAsync<T>(string group,
            CancellationToken ct = default) where T : notnull;

        Task<IAsyncDisposable> SubscribeAsync<T>(string group, string key, T value, TimeSpan expiresAfter,
            CancellationToken ct = default) where T : notnull;

        Task UnsubscribeAsync(string group, string key,
            CancellationToken ct = default);
    }
}
