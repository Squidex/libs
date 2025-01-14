// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events.EntityFramework;

public sealed partial class EFEventStore<T>
{
    private const int MaxWriteAttempts = 20;

    public async Task AppendAsync(Guid commitId, string streamName, long expectedVersion, ICollection<EventData> events,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(streamName));
        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            return;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync(ct);
        var commitSet = context.Set<EFEventCommit>();

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
            Timestamp = timeProvider.GetUtcNow().UtcDateTime
        };

        for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
        {
            try
            {
                await commitSet.AddAsync(commit, ct);
                await context.SaveChangesAsync(ct);

                try
                {
                    commit.Position = await adapter.GetPositionAsync(context, ct);
                    await context.SaveChangesAsync(ct);
                }
                catch
                {
                    commitSet.Remove(commit);
                    await context.SaveChangesAsync(ct);
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
        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        var query = context.Set<EFEventCommit>().ByStream(filter);

        await query.ExecuteDeleteAsync(ct);
    }

    private static async Task<long> GetEventStreamOffsetAsync(DbSet<EFEventCommit> commitSet, string streamName)
    {
        var record = await commitSet
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
