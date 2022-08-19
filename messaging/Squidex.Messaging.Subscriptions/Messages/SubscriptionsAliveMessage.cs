// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions.Messages
{
    public sealed record SubscriptionsAliveMessage : SubscriptionsMessageBase
    {
        public List<Guid> SubscriptionIds { get; init; } = null!;
    }
}
