// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.Implementation;
using Squidex.Messaging.Implementation.InMemory;

namespace Squidex.Messaging
{
    public class InMemorySubscriptionStoreTests : SubscriptionStoreTestsBase
    {
        public override Task<ISubscriptionStore> CreateSubscriptionStoreAsync()
        {
            return Task.FromResult<ISubscriptionStore>(new InMemorySubscriptionStore());
        }
    }
}
