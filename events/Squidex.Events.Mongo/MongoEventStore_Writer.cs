// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson;
using MongoDB.Driver;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events.Mongo;

public partial class MongoEventStore
{
    private const int MaxWriteAttempts = 20;

    private static readonly BsonTimestamp EmptyTimestamp =
        new BsonTimestamp(0);

    private static readonly BulkWriteOptions BulkOrdered =
        new BulkWriteOptions { IsOrdered = true };

    public Task DeleteAsync(StreamFilter filter,
        CancellationToken ct = default)
    {
        return collection.DeleteManyAsync(queryStrategy.Filter(filter), ct);
    }

    public async Task AppendAsync(Guid commitId, string streamName, long expectedVersion, ICollection<EventData> events,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamName);
        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            return;
        }

        for (var attempt = 1; ; attempt++)
        {
            var currentVersion = await GetEventStreamOffsetAsync(streamName, ct);
            if (expectedVersion > EventsVersion.Any && expectedVersion != currentVersion)
            {
                throw new WrongEventVersionException(currentVersion, expectedVersion);
            }

            // Calculate the offset for each attempt, otherwise we would retry the same conflicting slot.
            var commit = BuildCommit(commitId, streamName, expectedVersion > EventsVersion.Any ? expectedVersion : currentVersion, events);

            try
            {
                await collection.InsertOneAsync(commit, cancellationToken: ct);
            }
            catch (MongoWriteException ex) when (IsConflict(ex))
            {
                if (attempt >= MaxWriteAttempts)
                {
                    throw new EventStoreConcurrencyException("Could not acquire a free slot for the commit within the provided time.", ex);
                }

                // Reduce the chance that the same writers collide again.
                await Task.Delay(Random.Shared.Next(attempt * 5), ct);
                continue;
            }

            // Depending on the query strategy, we confirm the write after the insert.
            await queryStrategy.CompleteAsync([commit.Id], ct);
            return;
        }
    }

    public async Task AppendUnsafeAsync(IEnumerable<EventCommit> commits,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commits);

        var writes = new List<WriteModel<MongoEventCommit>>();
        var writeIds = new List<Guid>();

        foreach (var commit in commits)
        {
            var document = BuildCommit(commit.Id, commit.StreamName, commit.Offset, commit.Events);

            writes.Add(new InsertOneModel<MongoEventCommit>(document));
            writeIds.Add(commit.Id);
        }

        if (writes.Count == 0)
        {
            return;
        }

        try
        {
            await collection.BulkWriteAsync(writes, BulkOrdered, ct);
        }
        catch
        {
            await queryStrategy.AbortAsync([.. writeIds]);
            throw;
        }

        await queryStrategy.CompleteAsync([.. writeIds], ct);
    }

    private static bool IsConflict(MongoWriteException ex)
    {
        // Code 16908292 only happens in ferret-db, when many writers insert into the same unique constraint concurrently.
        return ex.WriteError?.Category == ServerErrorCategory.DuplicateKey || ex.WriteError?.Code == 16908292;
    }

    private async Task<long> GetEventStreamOffsetAsync(string streamName,
        CancellationToken ct = default)
    {
        var document =
            await collection.Find(Filter.Eq(x => x.EventStream, streamName))
                .Project<BsonDocument>(Projection
                    .Include(x => x.EventStreamOffset)
                    .Include(x => x.EventsCount))
                .Sort(Sort.Descending(x => x.EventStreamOffset)).Limit(1)
                .FirstOrDefaultAsync(ct);

        if (document != null)
        {
            return document[nameof(MongoEventCommit.EventStreamOffset)].ToInt64() + document[nameof(MongoEventCommit.EventsCount)].ToInt64();
        }

        return EventsVersion.Empty;
    }

    private static MongoEventCommit BuildCommit(Guid commitId, string streamName, long expectedVersion, ICollection<EventData> events)
    {
        // The global position is also used to identify zombies.
        var mongoCommit = new MongoEventCommit
        {
            Id = commitId,
            Events = events.Select(MongoEvent.FromEventData).ToArray(),
            EventsCount = events.Count,
            EventStream = streamName,
            EventStreamOffset = expectedVersion,
            GlobalPosition = 0,
            Timestamp = EmptyTimestamp,
        };

        return mongoCommit;
    }
}
