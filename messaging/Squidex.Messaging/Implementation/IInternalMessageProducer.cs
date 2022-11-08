// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Implementation;

public interface IInternalMessageProducer
{
    Task ProduceAsync(ChannelName channel, object message, string? key = null,
        CancellationToken ct = default);
}
