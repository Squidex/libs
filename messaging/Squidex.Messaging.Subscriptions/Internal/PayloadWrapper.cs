// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions.Internal;

internal sealed class PayloadWrapper(object message) : IPayloadWrapper
{
    public object Message => message;

    public ValueTask<object> CreatePayloadAsync()
    {
        return new ValueTask<object>(message);
    }
}
