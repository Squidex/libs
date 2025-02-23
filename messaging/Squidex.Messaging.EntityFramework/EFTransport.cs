// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Internal;

namespace Squidex.Messaging.EntityFramework;

public sealed class EFTransport<T>(
    IMessagingDataProvider messagingDataProvider,
    IDbContextFactory<T> dbContextFactory,
    IOptions<EFTransportOptions> options,
    TimeProvider timeProvider,
    ILogger<EFTransport<T>> log)
    : IMessagingTransport where T : DbContext
{
    private readonly EFTransportOptions options = options.Value;

    public Task InitializeAsync(
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public async Task<IAsyncDisposable?> CreateChannelAsync(ChannelName channel, string instanceName, bool consume, ProducerOptions options,
        CancellationToken ct)
    {
        if (!consume)
        {
            return null;
        }

        var channelName = channel.Name;

        IAsyncDisposable? subscription = null;
        if (channel.Type == ChannelType.Topic)
        {
            var value = new EFSubscriptionValue
            {
                InstanceName = instanceName,
            };

            subscription = await messagingDataProvider.StoreAsync(channelName, instanceName, value, this.options.SubscriptionExpiration, ct);
        }

        IAsyncDisposable result = new EFTransportCleaner<T>(
            dbContextFactory,
            channelName,
            options.Timeout,
            options.Expires,
            this.options.UpdateInterval,
            log,
            timeProvider);

        if (subscription != null)
        {
            result = new AggregateAsyncDisposable(result, subscription);
        }

        return result;
    }

    public async Task ProduceAsync(ChannelName channel, string instanceName, TransportMessage transportMessage,
        CancellationToken ct)
    {
        IReadOnlyList<string> queues;

        var channelName = channel.Name;
        if (channel.Type == ChannelType.Queue)
        {
            queues = [channelName];
        }
        else
        {
            var subscribed = await messagingDataProvider.GetEntriesAsync<EFSubscriptionValue>(channelName, ct);

            queues = subscribed.Select(x => x.Value.InstanceName).ToList();
        }

        if (queues.Count == 0)
        {
            return;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        foreach (var queueName in queues)
        {
            var message = new EFMessage
            {
                Id = $"{channelName}_{transportMessage.Headers[HeaderNames.Id]}_{queueName}",
                ChannelName = channelName,
                QueueName = queueName,
                MessageData = transportMessage.Data,
                MessageHeaders = transportMessage.Headers.Serialize(),
                TimeToLive = transportMessage.Headers.GetTimeToLive(timeProvider),
            };

            await context.Set<EFMessage>().AddAsync(message, ct);
        }

        await context.SaveChangesAsync(ct);
    }

    public Task<IAsyncDisposable> SubscribeAsync(ChannelName channel, string instanceName, MessageTransportCallback callback,
    CancellationToken ct)
    {
        var queueFilter = channel.Type == ChannelType.Topic ? instanceName : null;

        var subscription =
            new EFSubscription<T>(
                callback,
                dbContextFactory,
                channel.Name,
                queueFilter,
                options,
                timeProvider,
                log);

        return Task.FromResult<IAsyncDisposable>(subscription);
    }
}
