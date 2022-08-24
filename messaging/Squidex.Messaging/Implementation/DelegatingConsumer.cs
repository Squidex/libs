// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting;
using Squidex.Messaging.Implementation.Scheduler;
using ITransportList = System.Collections.Generic.IEnumerable<Squidex.Messaging.ITransport>;

namespace Squidex.Messaging.Implementation
{
    internal sealed class DelegatingConsumer : IBackgroundProcess
    {
        private readonly string instanceName;
        private readonly string activity;
        private readonly ChannelName channel;
        private readonly List<IAsyncDisposable> openSubscriptions = new List<IAsyncDisposable>();
        private readonly ChannelOptions channelOptions;
        private readonly HandlerPipeline pipeline;
        private readonly IScheduler scheduler;
        private readonly ITransportSerializer transportSerializer;
        private readonly ITransport transportAdapter;
        private readonly ILogger<DelegatingConsumer> log;
        private IAsyncDisposable? channelDisposable;
        private bool isReleased;

        public string Name => $"Messaging.Consumer({channel.Name})";

        public ChannelName Channel => channel;

        public int Order => int.MaxValue;

        public DelegatingConsumer(
            ChannelName channel,
            HandlerPipeline pipeline,
            IInstanceNameProvider instanceName,
            ITransportList transportList,
            ITransportSerializer transportSerializer,
            IOptionsMonitor<ChannelOptions> channelOptions,
            ILogger<DelegatingConsumer> log)
        {
            activity = $"Messaging.Consume({channel.Name})";

            this.channel = channel;
            this.channelOptions = channelOptions.Get(channel.ToString());
            this.instanceName = instanceName.Name;
            this.pipeline = pipeline;
            this.scheduler = this.channelOptions.Scheduler ?? InlineScheduler.Instance;
            this.transportAdapter = this.channelOptions.SelectTransport(transportList, channel);
            this.transportSerializer = transportSerializer;
            this.log = log;
        }

        public async Task StartAsync(
            CancellationToken ct)
        {
            if (pipeline.HasHandlers)
            {
                // Manage the lifetime of the channel here, so we do not have to do it in the transport.
                channelDisposable = await transportAdapter.CreateChannelAsync(channel, instanceName, true, channelOptions, ct);

                for (var i = 0; i < channelOptions.NumSubscriptions; i++)
                {
                    var subscription = await transportAdapter.SubscribeAsync(channel, instanceName, OnMessageAsync, ct);

                    openSubscriptions.Add(subscription);
                }
            }
        }

        public async Task StopAsync(
            CancellationToken ct)
        {
            isReleased = true;

            foreach (var subscription in openSubscriptions)
            {
                await subscription.DisposeAsync();
            }

            if (channelDisposable != null)
            {
                await channelDisposable.DisposeAsync();
            }
        }

        private async Task OnScheduledMessage((Type type, TransportResult TtransportResult, IMessageAck Ack) args,
            CancellationToken ct)
        {
            var (type, transportResult, ack) = args;
            try
            {
                object? message = null;

                try
                {
                    message = transportSerializer.Deserialize(transportResult.Message.Data, type);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Failed to deserialize message with type {type}.", type);
                    return;
                }

                if (message == null)
                {
                    log.LogError("Failed to deserialize message with type {type}.", type);
                    return;
                }

                var handlers = pipeline.GetHandlers(type);

                foreach (var handler in handlers)
                {
                    if (isReleased)
                    {
                        return;
                    }

                    try
                    {
                        using (var cts = new CancellationTokenSource(channelOptions.Timeout))
                        {
                            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct))
                            {
                                await handler(message, linked.Token);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        continue;
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Failed to consume message for system {system} with type {type}.", Name, type);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to consume message with type {type}.", type);
            }
            finally
            {
                if (!isReleased)
                {
                    // Ignore cancellation, better to delete the message, even if cancelled.
                    await ack.OnSuccessAsync(transportResult, default);
                }
            }
        }

        private async Task OnMessageAsync(TransportResult transportResult, IMessageAck ack,
            CancellationToken ct)
        {
            if (isReleased)
            {
                return;
            }

            using (var trace = MessagingTelemetry.Activities.StartActivity(activity))
            {
                transportResult.Message.Headers.TryGetDateTime(HeaderNames.TimeCreated, out var created);

                if (created != default && trace?.Id != null)
                {
                    MessagingTelemetry.Activities.StartActivity("QueueTime", ActivityKind.Internal, trace.Id,
                        startTime: created)?.Stop();
                }

                var typeString = transportResult.Message.Headers?.GetValueOrDefault(HeaderNames.Type) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(typeString))
                {
                    // The message is broken, we cannot handle it, even if we would retry.
                    await ack.OnSuccessAsync(transportResult, default);

                    log.LogWarning("Message has no type header.");
                    return;
                }

                var type = Type.GetType(typeString);

                if (type == null)
                {
                    // The message is broken, we cannot handle it, even if we would retry.
                    await ack.OnSuccessAsync(transportResult, default);

                    log.LogWarning("Message has invalid or unknown type {type}.", typeString);
                    return;
                }

                await scheduler.ExecuteAsync((type, transportResult, ack), (args, ct) => OnScheduledMessage(args, ct), ct);
            }
        }
    }
}
