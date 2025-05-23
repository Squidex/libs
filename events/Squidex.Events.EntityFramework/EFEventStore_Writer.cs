// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using PhenX.EntityFrameworkCore.BulkInsert.Extensions;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events.EntityFramework;

public sealed partial class EFEventStore<T>
{
    private const int MaxWriteAttempts = 20;

    public async Task AppendUnsafeAsync(IEnumerable<EventCommit> commits,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        var commitSet = dbContext.Set<EFEventCommit>();

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
            });

        await dbContext.ExecuteBulkInsertAsync(efCommits, options =>
        {
            options.CopyGeneratedColumns = true;
        }, ctk: ct);

        var ids = commits.Select(x => x.Id).ToArray();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                await adapter.UpdatePositionsAsync(dbContext, ids, ct);
                await transaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        catch
        {
            await commitSet.Where(x => ids.Contains(x.Id))
                .ExecuteDeleteAsync(ct);
            throw;
        }
    }

    public async Task AppendAsync(Guid commitId, string streamName, long expectedVersion, ICollection<EventData> events,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(streamName));
        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        var commitSet = dbContext.Set<EFEventCommit>();

        var currentVersion = await GetEventStreamOffsetAsync(commitSet, streamName);
        if (expectedVersion >= -1 && expectedVersion != currentVersion)
        {
            throw new WrongEventVersionException(currentVersion, expectedVersion);
        }

        var newOffset =
            expectedVersion >= -1 ?
            expectedVersion :
            currentVersion;

        var commit = new EFEventCommit
        {
            Id = commitId,
            EventStream = streamName,
            EventStreamOffset = newOffset,
            EventsCount = events.Count,
            Events = events.Select(e => e.SerializeToJsonString()).ToArray(),
            Timestamp = timeProvider.GetUtcNow().UtcDateTime,
        };

        for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
        {
            try
            {
                await commitSet.AddAsync(commit, ct);
                await dbContext.SaveChangesAsync(ct);

                try
                {
                    await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
                    try
                    {
                        await adapter.UpdatePositionAsync(dbContext, commit.Id, ct);
                        await transaction.CommitAsync(ct);
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync(ct);
                        throw;
                    }
                }
                catch
                {
                    commitSet.Remove(commit);
                    await dbContext.SaveChangesAsync(ct);
                    throw;
                }

                return;
            }
            catch (Exception ex) when (adapter.IsDuplicateException(ex))
            {
                if (expectedVersion >= -1)
                {
                    currentVersion = await GetEventStreamOffsetAsync(commitSet, streamName);

                    throw new WrongEventVersionException(currentVersion, expectedVersion);
                }

                if (attempt >= MaxWriteAttempts)
                {
                    throw new TimeoutException("Could not acquire a free slot for the commit within the provided time.");
                }
            }
        }
    }

    public async Task DeleteAsync(StreamFilter filter,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Set<EFEventCommit>().WhereStreamMatches(filter)
            .ExecuteDeleteAsync(ct);
    }

    private static async Task<long> GetEventStreamOffsetAsync(DbSet<EFEventCommit> commitSet, string streamName)
    {
        var record =
            await commitSet
                .Where(x => x.Position != null)
                .Where(x => x.EventStream == streamName)
                .OrderByDescending(x => x.EventStreamOffset)
                .Select(x => new { x.EventStreamOffset, x.EventsCount })
                .FirstOrDefaultAsync();

        if (record == null)
        {
            return -1;
        }

        return record.EventStreamOffset + record.EventsCount;
    }
}
