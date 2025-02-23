// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Squidex.Hosting;

namespace Squidex.Messaging.EntityFramework;

internal sealed class EFTransportCleaner<T>(
    IDbContextFactory<T> dbContextFactory,
    string channelName,
    TimeSpan timeout,
    TimeSpan expires,
    TimeSpan updateInterval,
    ILogger log,
    TimeProvider timeProvider)
    : IAsyncDisposable where T : DbContext
{
    private readonly SimpleTimer timer = new SimpleTimer(async ct =>
    {
        var timestamp = timeProvider.GetUtcNow().UtcDateTime;
        var timedout = timestamp - timeout;

        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        var updated =
            await context.Set<EFMessage>()
                .Where(x => x.ChannelName == channelName && x.TimeHandled != null && x.TimeHandled < timedout)
                .ExecuteUpdateAsync(b => b
                    .SetProperty(x => x.TimeHandled, (DateTime?)null)
                    .SetProperty(x => x.TimeToLive, timestamp + expires),
                    ct);

        if (updated > 0)
        {
            log.LogInformation("{collectionName}: Items reset: {count}.", channelName, updated);
        }
    }, updateInterval, log);

    public ValueTask DisposeAsync()
    {
        return timer.DisposeAsync();
    }
}
