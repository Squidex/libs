// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using Squidex.Messaging.Internal;
using ITransportList = System.Collections.Generic.IEnumerable<Squidex.Messaging.ITransport>;

namespace Squidex.Messaging.Implementation
{
    public sealed class DelegatingProducer : IInternalMessageProducer
    {
        private readonly HashSet<string> initializedChannels = new HashSet<string>();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private readonly string instanceName;
        private readonly ITransportList transportList;
        private readonly ITransportSerializer transportSerializer;
        private readonly IOptionsMonitor<ChannelOptions> channelOptions;
        private readonly IClock clock;

        public DelegatingProducer(
            IInstanceNameProvider instanceName,
            ITransportList transportList,
            ITransportSerializer transportSerializer,
            IOptionsMonitor<ChannelOptions> channelOptions,
            IClock clock)
        {
            this.channelOptions = channelOptions;
            this.instanceName = instanceName.Name;
            this.transportList = transportList;
            this.transportSerializer = transportSerializer;
            this.clock = clock;
        }

        public async Task ProduceAsync(ChannelName channel, object message, string? key = null,
            CancellationToken ct = default)
        {
            Guard.NotNull(message, nameof(message));

            // Falls back to the default options if not configured.
            var options = channelOptions.Get(channel.ToString());

            // Resolve the transport dynamically, therefore do not use the constructor.
            var transportAdapter = options.SelectTransport(transportList, channel);

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
                var data = transportSerializer.Serialize(message);

                if (string.IsNullOrEmpty(key))
                {
                    key = Guid.NewGuid().ToString();
                }

                var typeName = message?.GetType().AssemblyQualifiedName;

                if (string.IsNullOrWhiteSpace(typeName))
                {
                    ThrowHelper.ArgumentException("Cannot calculate type name.", nameof(message));
                    return;
                }

                var headers = new TransportHeaders()
                    .Set(HeaderNames.Id, Guid.NewGuid())
                    .Set(HeaderNames.Type, typeName)
                    .Set(HeaderNames.TimeExpires, options.Expires)
                    .Set(HeaderNames.TimeTimeout, options.Timeout)
                    .Set(HeaderNames.TimeCreated, clock.UtcNow);

                var transportMessage = new TransportMessage(data, key, headers);

                await transportAdapter.ProduceAsync(channel, instanceName, transportMessage, ct);
            }
        }
    }
}
