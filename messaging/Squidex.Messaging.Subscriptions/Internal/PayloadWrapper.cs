// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions.Internal;

internal sealed class PayloadWrapper<T>(T message) : IPayloadWrapper<T> where T : notnull
{
    public T Message => message;

    public ValueTask<object> CreatePayloadAsync()
    {
        return new ValueTask<object>(message);
    }
}
