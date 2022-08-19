// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions
{
    public interface IMessageEvaluator
    {
        ValueTask<IEnumerable<Guid>> GetSubscriptionsAsync(object message);

        void SubscriptionAdded(Guid id, ISubscription subscription);

        void SubscriptionRemoved(Guid id, ISubscription subscription);
    }
}
