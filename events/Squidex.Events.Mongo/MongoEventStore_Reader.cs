﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using MongoDB.Driver;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events.Mongo;

public delegate bool EventPredicate(MongoEvent data);

public partial class MongoEventStore
{
    // Use a relatively small batch size to keep the memory pressure low.
    private static readonly FindOptions BatchingOptions =
        new FindOptions { BatchSize = 200 };

    public IEventSubscription CreateSubscription(IEventSubscriber<StoredEvent> subscriber, StreamFilter filter = default, StreamPosition position = default)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        if (CanUseChangeStreams)
        {
            return new MongoEventStoreSubscription(this, subscriber, filter, position);
        }
        else
        {
            return new PollingSubscription(this, subscriber, filter, position, options.Value.PollingInterval);
        }
    }

    public async Task<IReadOnlyList<StoredEvent>> QueryStreamAsync(string streamName, long afterStreamPosition = EventsVersion.Empty,
        CancellationToken ct = default)
    {
        var commits =
            await collection.Find(CreateFilter(StreamFilter.Name(streamName), afterStreamPosition))
                .ToListAsync(ct);

        var result = Convert(commits, afterStreamPosition);

        if ((commits.Count == 0 || commits[0].EventStreamOffset != afterStreamPosition) && afterStreamPosition > EventsVersion.Empty)
        {
            var filterBefore =
                Builders<MongoEventCommit>.Filter.And(
                    FilterBuilder.ByStream(StreamFilter.Name(streamName)),
                    Builders<MongoEventCommit>.Filter.Lt(x => x.EventStreamOffset, afterStreamPosition));

            commits =
                await collection.Find(filterBefore).SortByDescending(x => x.EventStreamOffset).Limit(1)
                    .ToListAsync(ct);

            result = Convert(commits, afterStreamPosition).ToList();
        }

        return result;
    }

    public async IAsyncEnumerable<StoredEvent> QueryAllReverseAsync(StreamFilter filter = default, DateTime timestamp = default, int take = int.MaxValue,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (take <= 0)
        {
            yield break;
        }

        ParsedStreamPosition lastPosition = timestamp;
        var findFilter = CreateFilter(filter, lastPosition);
        var findQuery =
            collection.Find(findFilter, BatchingOptions)
                .Limit(take).Sort(Sort.Descending(x => x.Timestamp).Ascending(x => x.EventStream));

        var taken = 0;
        using (var cursor = await findQuery.ToCursorAsync(ct))
        {
            while (taken < take && await cursor.MoveNextAsync(ct))
            {
                foreach (var current in cursor.Current)
                {
                    foreach (var @event in current.Filtered(lastPosition).Reverse())
                    {
                        yield return @event;

                        taken++;
                        if (taken == take)
                        {
                            yield break;
                        }
                    }
                }
            }
        }
    }

    public async IAsyncEnumerable<StoredEvent> QueryAllAsync(StreamFilter filter = default, StreamPosition position = default, int take = int.MaxValue,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (take <= 0 || position.ReadFromEnd)
        {
            yield break;
        }

        ParsedStreamPosition lastPosition = position;
        var findFilter = CreateFilter(filter, lastPosition);
        var findQuery =
            collection.Find(findFilter).SortBy(x => x.Timestamp).ThenByDescending(x => x.EventStream)
                .Limit(take);

        var taken = 0;

        await foreach (var current in findQuery.ToAsyncEnumerable(ct))
        {
            foreach (var @event in current.Filtered(lastPosition))
            {
                yield return @event;

                taken++;
                if (taken == take)
                {
                    yield break;
                }
            }
        }
    }

    private static List<StoredEvent> Convert(IEnumerable<MongoEventCommit> commits, long streamPosition)
    {
        return commits.OrderBy(x => x.EventStreamOffset).ThenBy(x => x.Timestamp).SelectMany(x => x.Filtered(streamPosition)).ToList();
    }

    private static FilterDefinition<MongoEventCommit> CreateFilter(StreamFilter filter, ParsedStreamPosition streamPosition)
    {
        return Filter.And(FilterBuilder.ByPosition(streamPosition), FilterBuilder.ByStream(filter));
    }

    private static FilterDefinition<MongoEventCommit> CreateFilter(StreamFilter filter, long streamPosition)
    {
        return Filter.And(FilterBuilder.ByStream(filter), FilterBuilder.ByOffset(streamPosition));
    }
}
