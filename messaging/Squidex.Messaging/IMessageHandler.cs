// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging
{
    public interface IMessageHandler<in T> : IMessageHandler
    {
        Task HandleAsync(T message,
            CancellationToken ct);
    }

    public interface IMessageHandler
    {
    }
}
