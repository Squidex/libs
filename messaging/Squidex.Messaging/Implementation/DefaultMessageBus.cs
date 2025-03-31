// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using Squidex.Messaging.Internal;

namespace Squidex.Messaging.Implementation;

internal sealed class DefaultMessageBus(IInternalMessageProducer internalProducer, IOptions<MessagingOptions> options) : IMessageBus
{
    private readonly RoutingCollection routing = new RoutingCollection(options.Value.Routing);

    public async Task PublishAsync(object message, string? key = null,
        CancellationToken ct = default)
    {
        Guard.NotNull(message, nameof(message));

        if (routing != null)
        {
            foreach (var (predicate, channel) in routing)
            {
                if (predicate(message))
                {
                    await PublishToChannelAsync(message, channel, key, ct);
                    return;
                }
            }
        }

        throw new InvalidOperationException("Cannot find a matching channel name.");
    }

    public Task PublishToChannelAsync(object message, ChannelName channel, string? key = null,
        CancellationToken ct = default)
    {
        Guard.NotNull(message, nameof(message));

        return internalProducer.ProduceAsync(channel, message, key, ct);
    }
}
