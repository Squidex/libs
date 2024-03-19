// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions;

public class Subscription<T> : ISubscription<T>
{
    public virtual ValueTask<bool> ShouldHandle(T message)
    {
        return new ValueTask<bool>(true);
    }
}
