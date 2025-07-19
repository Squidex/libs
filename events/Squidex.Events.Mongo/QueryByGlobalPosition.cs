// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;
using MongoDB.Driver.Linq;

#pragma warning disable RECS0022 // Empty general catch clauses suppresses any error

namespace Squidex.Events.Mongo;

internal sealed class QueryByGlobalPosition(IMongoCollection<MongoEventCommit> collection) : QueryStrategy
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(5);

    private IMongoCollection<MongoGlobalPosition> positionCollection;

    private static readonly FilterDefinitionBuilder<MongoGlobalPosition> PositionFilters =
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

    public override FilterDefinition<MongoEventCommit> FilterAfter(string name, long streamPosition)
    {
        // Also filter out zombies.
        return Filters.And(base.FilterAfter(name, streamPosition), Filters.Gt(x => x.GlobalPosition, 0));
    }

    public override FilterDefinition<MongoEventCommit> FilterBefore(string name, long streamPosition)
    {
        // Also filter out zombies.
        return Filters.And(base.FilterBefore(name, streamPosition), Filters.Gt(x => x.GlobalPosition, 0));
    }

    public override FilterDefinition<ChangeStreamDocument<MongoEventCommit>> ByFilterInStream(StreamFilter filter)
    {
        // Also filter out zombies.
        return FiltersIsStream.And(base.ByFilterInStream(filter), FiltersIsStream.Gt(x => x.FullDocument.GlobalPosition, 0));
    }

    public override FilterDefinition<MongoEventCommit> FilterAfter(StreamFilter filter, ParsedStreamPosition streamPosition)
    {
        var globalPosition = Math.Max(1, streamPosition.GlobalPosition);

        var byPosition =
            streamPosition.IsEndOfCommit ?
                Filters.Gt(x => x.GlobalPosition, globalPosition) :
                Filters.Gte(x => x.GlobalPosition, globalPosition);

        return Filters.And(byPosition, ByStream(filter));
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
        var lockOwner = Guid.NewGuid();
        try
        {
            MongoGlobalPosition? position = null;
            using (var cts = new CancellationTokenSource(LockTimeout))
            {
                using var ctsLinked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
                try
                {
                    // Exponential Backoff
                    var delayMs = 1;

                    var now = DateTime.UtcNow;
                    while (!ctsLinked.Token.IsCancellationRequested)
                    {
                        position =
                            await positionCollection.FindOneAndUpdateAsync(
                                PositionFilters
                                    .And(
                                        PositionFilters.Eq(x => x.Id, 0),
                                        PositionFilters.Or(
                                            PositionFilters.Eq(x => x.LockTaken, null),
                                            PositionFilters.Lt(x => x.LockTaken, now))),
                                PositionUpdate
                                    .Set(x => x.LockTaken, now.Add(LockTimeout))
                                    .Set(x => x.LockOwner, lockOwner)
                                    .Inc(x => x.Position, ids.Length),
                                cancellationToken: ctsLinked.Token);

                        if (position != null)
                        {
                            break;
                        }

                        await Task.Delay(delayMs, ctsLinked.Token);

                        // Cap the delay at 200ms.
                        delayMs = Math.Min(delayMs * 2, 200);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (position == null)
            {
                throw new InvalidOperationException("Failed to get position lock.");
            }

            var writes = ids.Select((x, i) =>
                new UpdateOneModel<MongoEventCommit>(
                    Filters.Eq(x => x.Id, x),
                    Builders<MongoEventCommit>.Update.Set(x => x.GlobalPosition, position.Position + i)));

            // Do not use a cancellation token, because the hard part is actually done and it would be a waste.
            await collection.BulkWriteAsync(writes, cancellationToken: default);
        }
        catch
        {
            try
            {
                // Do not use a cancellation token to ensure that we get rid of zombies.
                await collection.DeleteManyAsync(x => x.GlobalPosition == 0, default);
            }
            catch
            {
                // Throw original exception.
            }

            throw;
        }
        finally
        {
            try
            {
                // Ensure that we own the lock and not someone else, because there was a timeout.
                await positionCollection.UpdateOneAsync(
                    PositionFilters.And(
                        PositionFilters.Eq(x => x.Id, 0),
                        PositionFilters.Eq(x => x.LockOwner, lockOwner)),
                    PositionUpdate.Set(x => x.LockTaken, null),
                    cancellationToken: default);
            }
            catch
            {
                // The main part is done.
            }
        }
    }
}
