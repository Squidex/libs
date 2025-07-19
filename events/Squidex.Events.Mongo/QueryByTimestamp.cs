// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;

namespace Squidex.Events.Mongo;

internal class QueryByTimestamp : QueryStrategy
{
    public override Task InitializeAsync(IMongoCollection<MongoEventCommit> collection, CancellationToken ct)
    {
        return collection.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<MongoEventCommit>(
                    Builders<MongoEventCommit>.IndexKeys
                        .Ascending(x => x.EventStream)
                        .Ascending(x => x.Timestamp)),
                new CreateIndexModel<MongoEventCommit>(
                    Builders<MongoEventCommit>.IndexKeys
                        .Descending(x => x.Timestamp)
                        .Ascending(x => x.EventStream)),
            ],
            ct);
    }

    public override SortDefinition<MongoEventCommit> SortAscending()
    {
        return Sort.Ascending(x => x.Timestamp).Descending(x => x.EventStream);
    }

    public override SortDefinition<MongoEventCommit> SortDescending()
    {
        return Sort.Descending(x => x.Timestamp).Ascending(x => x.EventStream);
    }

    public override FilterDefinition<MongoEventCommit> FilterAfter(StreamFilter filter, ParsedStreamPosition streamPosition)
    {
        var byTimestamp =
            streamPosition.IsEndOfCommit ?
                Filters.Gt(x => x.Timestamp, streamPosition.Timestamp) :
                Filters.Gte(x => x.Timestamp, streamPosition.Timestamp);

        return Filters.And(byTimestamp, ByStream(filter));
    }

    public override IEnumerable<StoredEvent> Filtered(MongoEventCommit commit, long position)
    {
        for (long i = 0, streamOffset = commit.EventStreamOffset + 1; i < commit.Events.Length; i++, streamOffset++)
        {
            var @event = commit.Events[i];
            if (streamOffset > position)
            {
                yield return Convert(commit, @event, i, streamOffset);
            }
        }
    }

    public override IEnumerable<StoredEvent> Filtered(MongoEventCommit commit, ParsedStreamPosition position)
    {
        for (long i = 0, streamOffset = commit.EventStreamOffset + 1; i < commit.Events.Length; i++, streamOffset++)
        {
            var @event = commit.Events[i];
            if (i > position.CommitOffset || commit.Timestamp > position.Timestamp)
            {
                yield return Convert(commit, @event, i, streamOffset);
            }
        }
    }

    private static StoredEvent Convert(MongoEventCommit commit, MongoEvent @event, long commitOffset, long eventStreamNumber)
    {
        var eventPosition =
            new ParsedStreamPosition(
                commit.Timestamp,
                commit.GlobalPosition,
                commitOffset,
                commit.Events.Length);

        return new StoredEvent(
            commit.EventStream,
            eventPosition.ToTimestampPosition(),
            eventStreamNumber,
            @event.ToEventData());
    }
}
