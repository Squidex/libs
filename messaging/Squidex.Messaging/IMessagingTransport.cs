// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name

using Squidex.Hosting;

namespace Squidex.Messaging
{
    public delegate Task MessageTransportCallback(TransportResult transportResult, IMessageAck ack,
            CancellationToken ct);

    public interface IMessagingTransport : IInitializable
    {
        Task<IAsyncDisposable?> CreateChannelAsync(ChannelName channel, string instanceName, bool consume, ProducerOptions options,
            CancellationToken ct);

        Task ProduceAsync(ChannelName channel, string instanceName, TransportMessage transportMessage,
            CancellationToken ct);

        Task<IAsyncDisposable> SubscribeAsync(ChannelName channel, string instanceName, MessageTransportCallback callback,
            CancellationToken ct);

        Task<IAsyncDisposable?> CreateCleanerAsync(ChannelName channel, string instanceName, TimeSpan timeout, TimeSpan expires,
            CancellationToken ct)
        {
            return Task.FromResult<IAsyncDisposable?>(null);
        }
    }
}
