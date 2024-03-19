// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions.Messages;

public abstract record PayloadMessageBase
{
    public string? SourceId { get; init; }

    public List<string> SubscriptionIds { get; init; } = default!;

    // This is a method, so it does not get serialized.
    public abstract object? GetUntypedPayload();
}
