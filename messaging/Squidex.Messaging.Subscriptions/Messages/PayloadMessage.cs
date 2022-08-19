// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions.Messages
{
    public sealed record PayloadMessage<T> : PayloadMessageBase where T : notnull
    {
        public T Payload { get; init; } = default!;

        public override object GetUntypedPayload()
        {
            return Payload;
        }
    }
}
