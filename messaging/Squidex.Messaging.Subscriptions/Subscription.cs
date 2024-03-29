﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions;

public class Subscription : ISubscription
{
    public virtual ValueTask<bool> ShouldHandle(object message)
    {
        return new ValueTask<bool>(true);
    }
}
