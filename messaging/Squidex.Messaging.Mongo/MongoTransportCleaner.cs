// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Squidex.Hosting;

namespace Squidex.Messaging.Mongo;

internal sealed class MongoTransportCleaner(
    IMongoCollection<MongoMessage> collection,
    string channelName,
    TimeSpan timeout,
    TimeSpan expires,
    TimeSpan updateInterval,
    ILogger log,
    TimeProvider timeProvider)
    : IAsyncDisposable
{
    private readonly SimpleTimer timer = new SimpleTimer(async ct =>
    {
        var timestamp = timeProvider.GetUtcNow().UtcDateTime;
        var timedout = timestamp - timeout;

        var update = await collection.UpdateManyAsync(x => x.TimeHandled != null && x.TimeHandled < timedout,
            Builders<MongoMessage>.Update
                .Set(x => x.TimeHandled, null)
                .Set(x => x.TimeToLive, timestamp + expires)
                .Set(x => x.PrefetchId, null),
            cancellationToken: ct);

        if (update.IsModifiedCountAvailable && update.ModifiedCount > 0)
        {
            log.LogInformation("{collectionName}: Items reset: {count}.", channelName, update.ModifiedCount);
        }
    }, updateInterval, log);

    public ValueTask DisposeAsync()
    {
        return timer.DisposeAsync();
    }
}
