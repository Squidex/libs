// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;

namespace Squidex.Events.Mongo;

internal sealed class QueryByGlobalPosition(IMongoCollection<MongoEventCommit> collection) : QueryStrategy
{
    private IMongoCollection<MongoGlobalPosition> positionCollection;

    private static readonly FilterDefinitionBuilder<MongoGlobalPosition> PositionFilter =
        Builders<MongoGlobalPosition>.Filter;

    private static readonly UpdateDefinitionBuilder<MongoGlobalPosition> PositionUpdate =
        Builders<MongoGlobalPosition>.Update;

    public override async Task InitializeAsync(IMongoCollection<MongoEventCommit> collection, CancellationToken ct)
    {
        positionCollection =
            collection.Database.GetCollection<MongoGlobalPosition>(
                $"{collection.CollectionNamespace.CollectionName}_Position",
                new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });

        try
        {
            // Start with one so that we can filter out zombies.
            await positionCollection.InsertOneAsync(
                new MongoGlobalPosition { Position = 1 },
                cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Already inserted.
        }

        await collection.Indexes.CreateManyAsync(
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

    public override IEnumerable<StoredEvent> Filtered(MongoEventCommit commit, long position)
    {
        for (long offset = 0, streamOffset = commit.EventStreamOffset + 1; offset < commit.Events.Length; offset++, streamOffset++)
        {
            var @event = commit.Events[offset];
            if (streamOffset > position)
            {
                yield return Convert(commit, @event, offset, streamOffset);
            }
        }
    }

    public override IEnumerable<StoredEvent> Filtered(MongoEventCommit commit, ParsedStreamPosition position)
    {
        for (long offset = 0, streamOffset = commit.EventStreamOffset + 1; offset < commit.Events.Length; offset++, streamOffset++)
        {
            var @event = commit.Events[offset];
            if (offset > position.CommitOffset || commit.GlobalPosition > position.GlobalPosition)
            {
                yield return Convert(commit, @event, offset, streamOffset);
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
            eventPosition.ToGlobalPosition(),
            eventStreamNumber,
            @event.ToEventData());
    }

    public override async Task CompleteAsync(Guid[] ids,
        CancellationToken ct)
    {
        try
        {
            MongoGlobalPosition? position = null;
            try
            {
                var now = DateTime.UtcNow;
                while (!ct.IsCancellationRequested)
                {
                    position =
                        await positionCollection.FindOneAndUpdateAsync(
                            PositionFilter
                                .And(
                                    PositionFilter.Eq(x => x.Id, 0),
                                    PositionFilter.Or(
                                        PositionFilter.Eq(x => x.LockTaken, null),
                                        PositionFilter.Lt(x => x.LockTaken, now))),
                            PositionUpdate
                                .Set(x => x.LockTaken, now.AddMinutes(5))
                                .Inc(x => x.Position, ids.Length),
                            cancellationToken: ct);

                    if (position != null)
                    {
                        break;
                    }

                    await Task.Delay(5, ct);
                }
            }
            catch (OperationCanceledException)
            {
            }

            if (position == null)
            {
                throw new InvalidOperationException("Failed to get position lock.");
            }

            var writes = ids.Select((x, i) =>
                new UpdateOneModel<MongoEventCommit>(
                    Filter.Eq(x => x.Id, x),
                    Builders<MongoEventCommit>.Update.Set(x => x.GlobalPosition, position.Position + i)));

            // Do not use a cancellation token, because the hard part is actually done and it would be a waste.
            await collection.BulkWriteAsync(writes, cancellationToken: default);
        }
        catch
        {
            // Do not use a cancellation token to ensure that we get rid of zombies.
            await collection.DeleteManyAsync(x => x.GlobalPosition == 0, default);
        }
        finally
        {
            // Do not use a cancellation token to ensure that unlock the position.
            await positionCollection.UpdateOneAsync(
                x => x.Id == 0,
                PositionUpdate.Set(x => x.LockTaken, null),
                cancellationToken: default);
        }
    }
}
