// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.Implementation.InMemory;

namespace Squidex.Messaging
{
    public class InMemorySubscriptionStoreTests : SubscriptionStoreTestsBase
    {
        public override Task<IMessagingSubscriptionStore> CreateSubscriptionStoreAsync()
        {
            return Task.FromResult<IMessagingSubscriptionStore>(new InMemorySubscriptionStore());
        }
    }
}
