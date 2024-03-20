﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.Subscriptions;
using Squidex.Messaging.Subscriptions.Implementation;

namespace Squidex.Messaging;

public class InMemorySubscriptionStoreTests : SubscriptionStoreTestsBase
{
    public override Task<IMessagingDataStore> CreateSubscriptionStoreAsync()
    {
        return Task.FromResult<IMessagingDataStore>(new InMemoryMessagingDataStore());
    }
}
