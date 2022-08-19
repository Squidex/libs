﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Implementation.Null
{
    public sealed class NullTransport : ITransport
    {
        public Task InitializeAsync(
            CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task CreateChannelAsync(ChannelName channel, ProducerOptions options,
            CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task ProduceAsync(ChannelName channel, TransportMessage transportMessage,
            CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<IAsyncDisposable> SubscribeAsync(ChannelName channel, string instanceName, MessageTransportCallback callback,
            CancellationToken ct)
        {
            return Task.FromResult<IAsyncDisposable>(new DelegateAsyncDisposable(() => default));
        }
    }
}
