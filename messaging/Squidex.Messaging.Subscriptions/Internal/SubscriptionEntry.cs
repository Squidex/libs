// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions.Internal;

internal sealed class SubscriptionEntry
{
    public ISubscription Subscription { get; }

    public Guid SubscriptionId { get; }

    public DateTime ExpiresUtc { get; private set; }

    public SubscriptionEntry(Guid subscriptionId, ISubscription subscription, DateTime expiresUtc)
    {
        SubscriptionId = subscriptionId;
        Subscription = subscription;
        ExpiresUtc = expiresUtc;
    }

    public void UpdateExpiration(DateTime expires)
    {
        ExpiresUtc = expires;
    }
}
