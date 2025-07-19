// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;

namespace Squidex.Events.Mongo;

internal class QueryByGlobalPosition(IMongoCollection<MongoEventCommit> collection) : QueryStrategy
{
    private IMongoCollection<MongoGlobalPosition> positionCollection;

    public override Task InitializeAsync(IMongoCollection<MongoEventCommit> collection, CancellationToken ct)
    {
        positionCollection =
            collection.Database.GetCollection<MongoGlobalPosition>(
                $"{collection.CollectionNamespace.CollectionName}_Position",
                new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });

        return collection.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<MongoEventCommit>(
                    Builders<MongoEventCommit>.IndexKeys
                        .Ascending(x => x.EventStream)
                        .Ascending(x => x.GlobalPosition)),
                new CreateIndexModel<MongoEventCommit>(
                    Builders<MongoEventCommit>.IndexKeys
                        .Descending(x => x.GlobalPosition)
                        .Ascending(x => x.EventStream)),
            ],
            ct);
    }

    public override SortDefinition<MongoEventCommit> SortAscending()
    {
        return Sort.Ascending(x => x.GlobalPosition).Descending(x => x.EventStream);
    }

    public override SortDefinition<MongoEventCommit> SortDescending()
    {
        return Sort.Descending(x => x.GlobalPosition).Ascending(x => x.EventStream);
    }

    public override FilterDefinition<MongoEventCommit> ByNameAfter(string name, long streamPosition)
    {
        // Also filter out zombies.
        return Filter.And(base.ByNameAfter(name, streamPosition), Filter.Gt(x => x.GlobalPosition, 0));
    }

    public override FilterDefinition<MongoEventCommit> ByNameBefore(string name, long streamPosition)
    {
        // Also filter out zombies.
        return Filter.And(base.ByNameBefore(name, streamPosition), Filter.Gt(x => x.GlobalPosition, 0));
    }

    public override FilterDefinition<ChangeStreamDocument<MongoEventCommit>> ByFilterInStream(StreamFilter filter)
    {
        // Also filter out zombies.
        return FilterInStream.And(base.ByFilterInStream(filter), FilterInStream.Gt(x => x.FullDocument.GlobalPosition, 0));
    }

    public override FilterDefinition<MongoEventCommit> ByFilter(StreamFilter filter, ParsedStreamPosition streamPosition)
    {
        var globalPosition = Math.Max(1, streamPosition.GlobalPosition);

        var byPosition =
            streamPosition.IsEndOfCommit ?
                Filter.Gt(x => x.GlobalPosition, globalPosition) :
                Filter.Gte(x => x.GlobalPosition, globalPosition);

        return Filter.And(byPosition, ByStream(filter));
    }

    public override IEnumerable<StoredEvent> Filtered(MongoEventCommit commit, ParsedStreamPosition position)
    {
        var eventStreamOffset = commit.EventStreamOffset;

        var commitPosition = commit.GlobalPosition;
        var commitOffset = 0;

        foreach (var @event in commit.Events)
        {
            eventStreamOffset++;

            if (commitOffset > position.CommitOffset || commit.GlobalPosition < position.GlobalPosition)
            {
                var eventData = @event.ToEventData();
                var eventPosition = new ParsedStreamPosition(commit.Timestamp, commitPosition, commitOffset, commit.Events.Length);

                yield return new StoredEvent(commit.EventStream, eventPosition, eventStreamOffset, eventData);
            }

            commitOffset++;
        }
    }

    public override async Task CompleteAsync(Guid[] ids,
        CancellationToken ct)
    {
        try
        {
            using var session = await collection.Database.Client.StartSessionAsync(cancellationToken: ct);

            // Assigns the IDs in a single transaction so that the monotonic order is guaranteed.
            await session.WithTransactionAsync(async (session, ct) =>
            {
                var positionDocument =
                    await positionCollection.FindOneAndUpdateAsync(
                        Builders<MongoGlobalPosition>.Filter.Eq(x => x.Id, 0),
                        Builders<MongoGlobalPosition>.Update
                            .Inc(x => x.Position, ids.Length),
                        new FindOneAndUpdateOptions<MongoGlobalPosition>
                        {
                            IsUpsert = true,
                        },
                        ct);

                // Start with one so that we can filter out zombies.
                var startPosition = 1 + positionDocument.Position - ids.Length;

                var writes = ids.Select((x, i) =>
                    new UpdateOneModel<MongoEventCommit>(
                        Filter.Eq(x => x.Id, x),
                        Builders<MongoEventCommit>.Update.Set(x => x.GlobalPosition, startPosition + i )));
                await collection.BulkWriteAsync(writes, cancellationToken: ct);
                return true;
            }, null, ct);
        }
        catch
        {
            // Do not use a cancellation token to ensure that we get rid of zombies.
            await collection.DeleteManyAsync(x => x.GlobalPosition == 0, default);
        }
    }
}
