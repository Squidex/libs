// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Messaging;

public record struct SubscribeRequest(string Group, string Key, SerializedObject Value, DateTime Expiration);

public interface IMessagingSubscriptionStore
{
    Task<IReadOnlyList<(string Key, SerializedObject Value, DateTime Expiration)>> GetSubscriptionsAsync(string group,
        CancellationToken ct);

    Task SubscribeManyAsync(SubscribeRequest[] requests,
        CancellationToken ct);

    Task UnsubscribeAsync(string group, string key,
        CancellationToken ct);

    Task CleanupAsync(DateTime now,
        CancellationToken ct);
}
