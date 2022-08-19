// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions.Messages
{
    public abstract record SubscribeMessageBase : SubscriptionsMessageBase
    {
        public Guid SubscriptionId { get; init; }

        // This is a method, so it does not get serialized.
        public abstract ISubscription GetUntypedSubscription();
    }
}