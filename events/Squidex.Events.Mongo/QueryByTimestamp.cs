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

    public override FilterDefinition<MongoEventCommit> ByFilter(StreamFilter filter, ParsedStreamPosition streamPosition)
    {
        var byTimestamp =
            streamPosition.IsEndOfCommit ?
                Filter.Gt(x => x.Timestamp, streamPosition.Timestamp) :
                Filter.Gte(x => x.Timestamp, streamPosition.Timestamp);

        return Filter.And(byTimestamp, ByStream(filter));
    }

    public override IEnumerable<StoredEvent> Filtered(MongoEventCommit commit, ParsedStreamPosition position)
    {
        var eventStreamOffset = commit.EventStreamOffset;

        var commitTimestamp = commit.Timestamp;
        var commitOffset = 0;

        foreach (var @event in commit.Events)
        {
            eventStreamOffset++;

            if (commitOffset > position.CommitOffset || commitTimestamp > position.Timestamp)
            {
                var eventData = @event.ToEventData();
                var eventPosition = new ParsedStreamPosition(commitTimestamp, commit.GlobalPosition, commitOffset, commit.Events.Length);

                yield return new StoredEvent(commit.EventStream, eventPosition, eventStreamOffset, eventData);
            }

            commitOffset++;
        }
    }
}
