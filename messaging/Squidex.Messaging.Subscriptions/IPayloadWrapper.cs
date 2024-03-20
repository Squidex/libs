// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions;

public interface IPayloadWrapper<T> where T : notnull
{
    T Message { get; }

    ValueTask<object> CreatePayloadAsync();
}
