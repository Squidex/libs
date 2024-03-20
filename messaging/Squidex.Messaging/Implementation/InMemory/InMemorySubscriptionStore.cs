// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;

#pragma warning disable RECS0082 // Parameter has the same name as a member and hides it
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Messaging.Implementation.InMemory;

public class InMemorySubscriptionStore : IMessagingSubscriptionStore
{
    private readonly ConcurrentDictionary<(string Group, string Key), Subscription> subscriptions = [];

    private sealed record Subscription(SerializedObject Value, DateTime Expiration);

    public Task<IReadOnlyList<(string Key, SerializedObject Value, DateTime Expiration)>> GetSubscriptionsAsync(string group,
        CancellationToken ct)
    {
        var result = new List<(string Key, SerializedObject Value, DateTime Expiration)>();

        foreach (var (key, subscription) in subscriptions.Where(x => x.Key.Group == group))
        {
            result.Add((key.Key, subscription.Value, subscription.Expiration));
        }

        return Task.FromResult<IReadOnlyList<(string Key, SerializedObject Value, DateTime Expiration)>>(result);
    }

    public Task SubscribeManyAsync(SubscribeRequest[] requests,
        CancellationToken ct)
    {
        foreach (var (group, key, value, expiration) in requests)
        {
            subscriptions[(group, key)] = new Subscription(value, expiration);
        }

        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string group, string key,
        CancellationToken ct)
    {
        subscriptions.TryRemove((group, key), out _);

        return Task.CompletedTask;
    }

    public Task CleanupAsync(DateTime now,
        CancellationToken ct)
    {
        // ToList on concurrent dictionary is not thread safe, therefore we maintain our own local copy.
        HashSet<(string Group, string Key)>? toRemove = null;

        foreach (var (key, subscription) in subscriptions)
        {
            if (subscription.Expiration < now)
            {
                toRemove ??= [];
                toRemove.Add(key);
            }
        }

        if (toRemove != null)
        {
            foreach (var key in toRemove)
            {
                subscriptions.TryRemove(key, out _);
            }
        }

        return Task.CompletedTask;
    }
}
