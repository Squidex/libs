// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable RECS0022 // Empty general catch clauses suppresses any error

namespace Squidex.Events.EntityFramework;

public sealed partial class EFEventStore<T>
{
    private const int MaxWriteAttempts = 20;

    public async Task AppendUnsafeAsync(IEnumerable<EventCommit> commits,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commits);

        var timestamp = timeProvider.GetUtcNow().UtcDateTime;

        var efCommits = commits.Select(x =>
            new EFEventCommit
            {
                Id = x.Id,
                EventStream = x.StreamName,
                EventStreamOffset = x.Offset,
                EventsCount = x.Events.Count,
                Events = x.Events.Select(e => e.SerializeToJsonString()).ToArray(),
                Timestamp = timestamp,
            }).ToList();

        if (efCommits.Count == 0)
        {
            return;
        }

        var ids = efCommits.Select(x => x.Id).ToArray();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await bulkInserter.BulkInsertAsync(dbContext, efCommits, ct);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            await adapter.UpdatePositionsAsync(dbContext, ids, ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            try
            {
                // Do not use the cancellation token to ensure that we get rid of zombies.
                await dbContext.Set<EFEventCommit>().Where(x => ids.Contains(x.Id) && x.Position == null)
                    .ExecuteDeleteAsync(CancellationToken.None);
            }
            catch
            {
                // Throw original exception.
            }

            throw;
        }
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

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        var commitSet = dbContext.Set<EFEventCommit>();

        var serializedEvents = events.Select(e => e.SerializeToJsonString()).ToArray();

        for (var attempt = 1; ; attempt++)
        {
            var currentVersion = await GetEventStreamOffsetAsync(commitSet, streamName, ct);

            if (expectedVersion > EventsVersion.Any && expectedVersion != currentVersion)
            {
                throw new WrongEventVersionException(currentVersion, expectedVersion);
            }

            // Calculate the offset for each attempt, otherwise we would retry the same conflicting slot.
            var commit = new EFEventCommit
            {
                Id = commitId,
                EventStream = streamName,
                EventStreamOffset = expectedVersion > EventsVersion.Any ? expectedVersion : currentVersion,
                EventsCount = events.Count,
                Events = serializedEvents,
                Timestamp = timeProvider.GetUtcNow().UtcDateTime,
            };

            // The insert is not part of the position transaction, because holding the unique index locks
            // until the global position is assigned causes a lot of lock contention and deadlocks.
            try
            {
                await commitSet.AddAsync(commit, ct);
                await dbContext.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (adapter.IsDuplicateException(ex))
            {
                if (attempt >= MaxWriteAttempts)
                {
                    throw new EventStoreConcurrencyException("Could not acquire a free slot for the commit within the provided time.", ex);
                }

                // A crash between insert and position update leaves a commit without position behind that blocks the slot.
                await DeleteZombiesAsync(commitSet, streamName, ct);

                // Reduce the chance that the same writers collide again.
                await Task.Delay(Random.Shared.Next(attempt * 5), ct);
                continue;
            }
            finally
            {
                // The commit must not be inserted again with the next attempt.
                dbContext.ChangeTracker.Clear();
            }

            try
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

                await adapter.UpdatePositionAsync(dbContext, commit.Id, ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                try
                {
                    // Do not use the cancellation token to ensure that we get rid of zombies.
                    await commitSet.Where(x => x.Id == commit.Id && x.Position == null)
                        .ExecuteDeleteAsync(CancellationToken.None);
                }
                catch
                {
                    // Throw original exception.
                }

                throw;
            }

            return;
        }
    }

    public async Task DeleteAsync(StreamFilter filter,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Set<EFEventCommit>().WhereStreamMatches(filter)
            .ExecuteDeleteAsync(ct);
    }

    private async Task DeleteZombiesAsync(DbSet<EFEventCommit> commitSet, string streamName,
        CancellationToken ct)
    {
        // Commits without position older than this cannot be in progress anymore and are left over from a crash.
        var zombieTimestamp = timeProvider.GetUtcNow().UtcDateTime.AddHours(-1);

        await commitSet
            .Where(x => x.EventStream == streamName)
            .Where(x => x.Position == null)
            .Where(x => x.Timestamp < zombieTimestamp)
            .ExecuteDeleteAsync(ct);
    }

    private static async Task<long> GetEventStreamOffsetAsync(DbSet<EFEventCommit> commitSet, string streamName,
        CancellationToken ct)
    {
        var record =
            await commitSet
                .Where(x => x.Position != null)
                .Where(x => x.EventStream == streamName)
                .OrderByDescending(x => x.EventStreamOffset)
                .Select(x => new { x.EventStreamOffset, x.EventsCount })
                .FirstOrDefaultAsync(ct);

        if (record == null)
        {
            return -1;
        }

        return record.EventStreamOffset + record.EventsCount;
    }
}
