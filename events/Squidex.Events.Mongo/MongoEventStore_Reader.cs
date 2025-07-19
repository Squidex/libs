// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using MongoDB.Driver;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events.Mongo;

public partial class MongoEventStore
{
    public IEventSubscription CreateSubscription(IEventSubscriber<StoredEvent> subscriber, StreamFilter filter = default, StreamPosition position = default)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        if (CanUseChangeStreams)
        {
            return new MongoEventStoreSubscription(this, subscriber, filter, position, queryStrategy);
        }

        return new PollingSubscription(this, subscriber, filter, position, options.Value.PollingInterval);
    }

    public async Task<IReadOnlyList<StoredEvent>> QueryStreamAsync(string streamName, long afterStreamPosition = EventsVersion.Empty,
        CancellationToken ct = default)
    {
        var commits =
            await collection.Find(queryStrategy.FilterAfter(streamName, afterStreamPosition))
                .ToListAsync(ct);

        var result = Convert(commits, afterStreamPosition);

        if ((commits.Count == 0 || commits[0].EventStreamOffset != afterStreamPosition) && afterStreamPosition > EventsVersion.Empty)
        {
            commits =
                await collection.Find(queryStrategy.FilterBefore(streamName, afterStreamPosition)).SortByDescending(x => x.EventStreamOffset).Limit(1)
                    .ToListAsync(ct);

            result = Convert(commits, afterStreamPosition).ToList();
        }

        return result;
    }

    public IAsyncEnumerable<StoredEvent> QueryAllReverseAsync(StreamFilter filter = default, DateTime timestamp = default, int take = int.MaxValue,
        CancellationToken ct = default)
    {
        if (take <= 0)
        {
            return Extensions.Empty<StoredEvent>();
        }

        async IAsyncEnumerable<StoredEvent> QueryCoreAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            ParsedStreamPosition lastPosition = timestamp;

            var findFilter = queryStrategy.FilterAfter(filter, lastPosition);
            var findQuery = collection.Find(findFilter).Limit(take).Sort(queryStrategy.SortDescending());

            await foreach (var commit in findQuery.ToAsyncEnumerable(ct))
            {
                foreach (var @event in queryStrategy.Filtered(commit, lastPosition).Reverse())
                {
                    yield return @event;
                }
            }
        }

        return QueryCoreAsync(ct).Take(take);
    }

    public IAsyncEnumerable<StoredEvent> QueryAllAsync(StreamFilter filter = default, StreamPosition position = default, int take = int.MaxValue,
        CancellationToken ct = default)
    {
        if (take <= 0 || position.ReadFromEnd)
        {
            return Extensions.Empty<StoredEvent>();
        }

        async IAsyncEnumerable<StoredEvent> QueryCoreAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            ParsedStreamPosition lastPosition = position;

            var findFilter = queryStrategy.FilterAfter(filter, lastPosition);
            var findQuery = collection.Find(findFilter).Limit(take).Sort(queryStrategy.SortAscending());

            await foreach (var commit in findQuery.ToAsyncEnumerable(ct))
            {
                foreach (var @event in queryStrategy.Filtered(commit, lastPosition))
                {
                    yield return @event;
                }
            }
        }

        return QueryCoreAsync(ct).Take(take);
    }

    private List<StoredEvent> Convert(IEnumerable<MongoEventCommit> commits, long streamPosition)
    {
        return commits.OrderBy(x => x.EventStreamOffset).ThenBy(x => x.Timestamp).SelectMany(x => queryStrategy.Filtered(x, streamPosition)).ToList();
    }
}
