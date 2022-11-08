// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions.Messages;

public sealed record SubscribeMessage<T> : SubscribeMessageBase where T : ISubscription
{
    public T Subscription { get; init; } = default!;

    public override ISubscription GetUntypedSubscription()
    {
        return Subscription;
    }
}
