// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;

namespace Squidex.Messaging.Subscriptions;

public sealed class DefaultMessageEvaluator : IMessageEvaluator
{
    private readonly ConcurrentDictionary<Guid, ISubscription> clusterSubscriptions = new ConcurrentDictionary<Guid, ISubscription>();

    public async ValueTask<IEnumerable<Guid>> GetSubscriptionsAsync(object message)
    {
        List<Guid>? result = null;

        foreach (var (id, subscription) in clusterSubscriptions)
        {
            if (await subscription.ShouldHandle(message))
            {
                result ??= [];
                result.Add(id);
            }
        }

        return result ?? Enumerable.Empty<Guid>();
    }

    public void SubscriptionAdded(Guid id, ISubscription subscription)
    {
        clusterSubscriptions[id] = subscription;
    }

    public void SubscriptionRemoved(Guid id, ISubscription subscription)
    {
        clusterSubscriptions.TryRemove(id, out _);
    }
}
