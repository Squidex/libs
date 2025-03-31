// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using Squidex.Messaging.Internal;
using IMessagingTransports = System.Collections.Generic.IEnumerable<Squidex.Messaging.IMessagingTransport>;

namespace Squidex.Messaging.Implementation;

public sealed class DelegatingProducer(
    IInstanceNameProvider instanceName,
    IMessagingTransports messagingTransports,
    IMessagingSerializer messagingSerializer,
    IOptionsMonitor<ChannelOptions> channelOptions,
    TimeProvider timeProvider) : IInternalMessageProducer
{
    private readonly HashSet<string> initializedChannels = [];
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
    private readonly string instanceName = instanceName.Name;

    public async Task ProduceAsync(ChannelName channel, object message, string? key = null,
        CancellationToken ct = default)
    {
        Guard.NotNull(message, nameof(message));

        // Falls back to the default options if not configured.
        var options = channelOptions.Get(channel.ToString());

        // Resolve the transport dynamically, therefore do not use the constructor.
        var transportAdapter = options.SelectTransport(messagingTransports, channel);

        await semaphore.WaitAsync(ct);
        try
        {
            if (initializedChannels.Add(channel.Name))
            {
                await transportAdapter.CreateChannelAsync(channel, instanceName, false, options, default);
            }
        }
        finally
        {
            semaphore.Release();
        }

        // Should be possible to avoid the allocations here.
        using (MessagingTelemetry.Activities.StartActivity($"Messaging.Produce({channel})"))
        {
            var (data, typeName, format) = messagingSerializer.Serialize(message);

            if (string.IsNullOrEmpty(key))
            {
                key = Guid.NewGuid().ToString();
            }

            var headers = new TransportHeaders()
                .Set(HeaderNames.Id, Guid.NewGuid())
                .Set(HeaderNames.Type, typeName)
                .Set(HeaderNames.Format, format)
                .Set(HeaderNames.TimeExpires, options.Expires)
                .Set(HeaderNames.TimeTimeout, options.Timeout)
                .Set(HeaderNames.TimeCreated, timeProvider.GetUtcNow().UtcDateTime);

            var transportMessage = new TransportMessage(data, key, headers);

            await transportAdapter.ProduceAsync(channel, instanceName, transportMessage, ct);
        }
    }
}
