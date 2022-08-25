// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging
{
    public sealed class MessagingOptions
    {
        public RoutingCollection Routing { get; set; } = new RoutingCollection();

        public RoutingCollection Topics { get; set; } = new RoutingCollection();

        public TimeSpan SubscriptionUpdateInterval { get; set; } = TimeSpan.FromSeconds(30);

        public TimeSpan SubscriptionCleanupInterval { get; set; } = TimeSpan.FromSeconds(60);
    }
}
