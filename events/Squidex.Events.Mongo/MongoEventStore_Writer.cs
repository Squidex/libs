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

    private static readonly BulkWriteOptions BulkUnordered =
        new BulkWriteOptions { IsOrdered = true };

    public Task DeleteAsync(StreamFilter filter,
        CancellationToken ct = default)
    {
        return collection.DeleteManyAsync(queryStrategy.ByFilter(filter, default), ct);
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

        var currentVersion = await GetEventStreamOffsetAsync(streamName, ct);

        if (expectedVersion > EventsVersion.Any && expectedVersion != currentVersion)
        {
            throw new WrongEventVersionException(currentVersion, expectedVersion);
        }

        var commit = BuildCommit(commitId, streamName, expectedVersion >= -1 ? expectedVersion : currentVersion, events);

        for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
        {
            try
            {
                await collection.InsertOneAsync(commit, cancellationToken: ct);
                await queryStrategy.CompleteAsync([commit.Id], ct);
                return;
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                currentVersion = await GetEventStreamOffsetAsync(streamName, ct);

                if (expectedVersion > EventsVersion.Any)
                {
                    throw new WrongEventVersionException(currentVersion, expectedVersion);
                }

                if (attempt >= MaxWriteAttempts)
                {
                    throw new TimeoutException("Could not acquire a free slot for the commit within the provided time.");
                }
            }
        }
    }

    public async Task AppendUnsafeAsync(IEnumerable<EventCommit> commits,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commits);

        var writes = new List<WriteModel<MongoEventCommit>>();

        foreach (var commit in commits)
        {
            var document = BuildCommit(commit.Id, commit.StreamName, commit.Offset, commit.Events);

            writes.Add(new InsertOneModel<MongoEventCommit>(document));
        }

        if (writes.Count > 0)
        {
            await collection.BulkWriteAsync(writes, BulkUnordered, ct);
            await queryStrategy.CompleteAsync(commits.Select(x => x.Id).ToArray(), ct);
        }
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
