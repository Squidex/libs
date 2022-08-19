// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions.Internal
{
    public interface IUntypedLocalSubscription
    {
        void OnError(Exception exception);

        void OnNext(object? value);
    }
}
