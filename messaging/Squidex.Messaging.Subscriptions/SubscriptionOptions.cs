// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions
{
    public sealed class SubscriptionOptions
    {
        public TimeSpan SubscriptionExpirationTime { get; set; } = TimeSpan.FromMinutes(30);

        public TimeSpan SubscriptionUpdateTime { get; set; } = TimeSpan.FromMinutes(5);
    }
}
