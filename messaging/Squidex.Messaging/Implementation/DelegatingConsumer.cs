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
using IMessagingTransports = System.Collections.Generic.IEnumerable<Squidex.Messaging.IMessagingTransport>;

namespace Squidex.Messaging.Implementation;

internal sealed class DelegatingConsumer : IBackgroundProcess
{
    private readonly string instanceName;
    private readonly string activity;
    private readonly List<IAsyncDisposable> openSubscriptions = new List<IAsyncDisposable>();
    private readonly ChannelName channelName;
    private readonly ChannelOptions channelOptions;
    private readonly HandlerPipeline pipeline;
    private readonly IScheduler scheduler;
    private readonly IMessagingSerializer messagingSerializer;
    private readonly IMessagingTransport messagingTransport;
    private readonly ILogger<DelegatingConsumer> log;
    private IAsyncDisposable? channelDisposable;
    private bool isReleased;

    public string Name => $"Messaging.Consumer({channelName.Name})";

    public ChannelName Channel => channelName;

    public int Order => int.MaxValue;

    public DelegatingConsumer(
        ChannelName channelName,
        HandlerPipeline pipeline,
        IInstanceNameProvider instanceName,
        IMessagingTransports messagingTransports,
        IMessagingSerializer messagingSerializer,
        IOptionsMonitor<ChannelOptions> channelOptions,
        ILogger<DelegatingConsumer> log)
    {
        activity = $"Messaging.Consume({channelName.Name})";

        this.channelName = channelName;
        this.channelOptions = channelOptions.Get(channelName.ToString());
        this.instanceName = instanceName.Name;
        this.messagingSerializer = messagingSerializer;
        this.messagingTransport = this.channelOptions.SelectTransport(messagingTransports, channelName);
        this.pipeline = pipeline;
        this.scheduler = this.channelOptions.Scheduler ?? InlineScheduler.Instance;
        this.log = log;
    }

    public async Task StartAsync(
        CancellationToken ct)
    {
        if (pipeline.HasHandlers)
        {
            // Manage the lifetime of the channel here, so we do not have to do it in the transport.
            channelDisposable = await messagingTransport.CreateChannelAsync(channelName, instanceName, true, channelOptions, ct);

            for (var i = 0; i < channelOptions.NumSubscriptions; i++)
            {
                var subscription = await messagingTransport.SubscribeAsync(channelName, instanceName, OnMessageAsync, ct);

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

    private async Task OnScheduledMessage((SerializedObject source, TransportResult transportResult, IMessageAck Ack) args,
        CancellationToken ct)
    {
        var (source, transportResult, ack) = args;
        try
        {
            (object Message, Type Type) deserialized;
            try
            {
                deserialized = messagingSerializer.Deserialize(source);
            }
            catch (Exception ex)
            {
                // The message is broken, we cannot handle it, even if we would retry.
                log.LogError(ex, "Failed to deserialize message with type {type}.", source.TypeString);
                return;
            }

            var handlers = pipeline.GetHandlers(deserialized.Type);

            if (handlers.Count == 0)
            {
                return;
            }

            var shouldLog = channelOptions.LogMessage?.Invoke(deserialized.Message) == true;

            foreach (var handler in handlers)
            {
                if (isReleased)
                {
                    return;
                }

                try
                {
                    if (shouldLog)
                    {
                        log.LogInformation("Handling {message} for {channel} with {handler}.", deserialized.Message, channelName, handler);
                    }

                    using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct))
                    {
                        linked.CancelAfter(channelOptions.Timeout);

                        await handler(deserialized.Message, linked.Token);
                    }

                    if (shouldLog)
                    {
                        log.LogInformation("Handled {message} for {channel} with {handler}.", deserialized.Message, channelName, handler);
                    }
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
                catch (Exception ex)
                {
                    log.LogInformation(ex, "Failed to handle {message} for {channel} with {handler}.", deserialized.Message, channelName, handler);
                }
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to consume message with type {type}.", source.TypeString);
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
                var start = new DateTimeOffset(created);

                MessagingTelemetry.Activities.StartActivity("QueueTime", ActivityKind.Internal, trace.Id, startTime: start)?.Stop();
            }

            var typeString = transportResult.Message.Headers?.GetValueOrDefault(HeaderNames.Type);

            if (string.IsNullOrWhiteSpace(typeString))
            {
                // The message is broken, we cannot handle it, even if we would retry.
                await ack.OnSuccessAsync(transportResult, default);

                log.LogWarning("Message has no type header.");
                return;
            }

            var serializedFormat = transportResult.Message.Headers?.GetValueOrDefault(HeaderNames.Format);
            var serializedObject = new SerializedObject(transportResult.Message.Data, typeString, serializedFormat);

            await scheduler.ExecuteAsync((serializedObject, transportResult, ack), OnScheduledMessage, ct);
        }
    }
}
