// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Squidex.Messaging.Internal;

namespace Squidex.Messaging.Mongo;

internal sealed class MongoTransportCleaner : IAsyncDisposable
{
    private readonly SimpleTimer timer;

    public MongoTransportCleaner(IMongoCollection<MongoMessage> collection, string collectionName,
        TimeSpan timeout,
        TimeSpan expires,
        TimeSpan updateInterval,
        ILogger log,
        TimeProvider timeProvider)
    {
        timer = new SimpleTimer(async ct =>
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            var timedout = now - timeout;

            var update = await collection.UpdateManyAsync(x => x.TimeHandled != null && x.TimeHandled < timedout,
                Builders<MongoMessage>.Update
                    .Set(x => x.TimeHandled, null)
                    .Set(x => x.TimeToLive, now + expires)
                    .Set(x => x.PrefetchId, null),
                cancellationToken: ct);

            if (update.IsModifiedCountAvailable && update.ModifiedCount > 0)
            {
                log.LogInformation("{collectionName}: Items reset: {count}.", collectionName, update.ModifiedCount);
            }
        }, updateInterval, log);
    }

    public ValueTask DisposeAsync()
    {
        return timer.DisposeAsync();
    }
}
