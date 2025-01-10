// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Squidex.Hosting;

namespace Squidex.Messaging.EntityFramework;

internal sealed class EFSubscription<T> : IAsyncDisposable, IMessageAck where T : DbContext
{
    private readonly string channelName;
    private readonly string? queueFilter;
    private readonly IDbContextFactory<T> dbContextFactory;
    private readonly TimeProvider timeProvider;
    private readonly SimpleTimer timer;
    private readonly ILogger log;

    public EFSubscription(
        MessageTransportCallback callback,
        IDbContextFactory<T> dbContextFactory,
        string channelName,
        string? queueFilter,
        EFTransportOptions options,
        TimeProvider timeProvider,
        ILogger log)
    {
        this.channelName = channelName;
        this.dbContextFactory = dbContextFactory;
        this.queueFilter = queueFilter;
        this.timeProvider = timeProvider;
        this.log = log;

        timer = new SimpleTimer(async ct =>
        {
            while (await PollMessageAsync(callback, ct))
            {
                // If we have received a message it is very likely to fetch another one, so we loop until the queue is empty.
            }
        }, options.PollingInterval, log);
    }

    private async Task<bool> PollMessageAsync(MessageTransportCallback callback,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        var query = context.Set<EFMessage>().Where(x =>
            x.ChannelName == channelName &&
            x.TimeHandled == null &&
            x.TimeToLive > now);

        if (queueFilter != null)
        {
            query = query.Where(x => x.QueueName == queueFilter);
        }

        var efMessage = await query.FirstOrDefaultAsync(ct);
        if (efMessage == null)
        {
            return false;
        }

        efMessage.TimeHandled = now;
        try
        {
            // Create an new version for concurrency checks.
            efMessage.Version = Guid.NewGuid();

            await context.SaveChangesAsync(ct);
            await callback(efMessage.ToTransportResult(), this, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // THe message is consumed by another process.
        }

        return true;
    }

    public ValueTask DisposeAsync()
    {
        return timer.DisposeAsync();
    }

    public async Task OnErrorAsync(TransportResult result,
        CancellationToken ct)
    {
        if (timer.IsDisposed)
        {
            return;
        }

        if (result.Data is not string id)
        {
            log.LogWarning("Transport message has no MongoDb ID.");
            return;
        }

        try
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(ct);

            await context.Set<EFMessage>()
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(b => b
                    .SetProperty(x => x.TimeHandled, (DateTime?)null),
                    ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to put the message back into the queue '{queue}'.", channelName);
        }
    }

    public async Task OnSuccessAsync(TransportResult result,
        CancellationToken ct)
    {
        if (timer.IsDisposed)
        {
            return;
        }

        if (result.Data is not string id)
        {
            log.LogWarning("Transport message has no MongoDb ID.");
            return;
        }

        try
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(ct);

            await context.Set<EFMessage>().Where(x => x.Id == id)
                .ExecuteDeleteAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to remove message from queue '{queue}'.", channelName);
        }
    }
}
