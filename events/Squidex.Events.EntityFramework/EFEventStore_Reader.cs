// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events.EntityFramework;

public sealed partial class EFEventStore<T> : IEventStore
{
    public IEventSubscription CreateSubscription(IEventSubscriber<StoredEvent> eventSubscriber, StreamFilter filter = default, StreamPosition position = default)
    {
        return new PollingSubscription(this, eventSubscriber, filter, position, options.Value.PollingInterval);
    }

    public async Task<IReadOnlyList<StoredEvent>> QueryStreamAsync(string streamName, long afterStreamPosition = -1,
        CancellationToken ct = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        var commits = await context.Set<EFEventCommit>()
            .ByStream(StreamFilter.Name(streamName))
            .ByOffset(afterStreamPosition)
            .ToListAsync(ct);

        var result = Convert(commits, afterStreamPosition);

        if ((commits.Count == 0 || commits[0].EventStreamOffset != afterStreamPosition) && afterStreamPosition > EventsVersion.Empty)
        {
            commits = await context.Set<EFEventCommit>()
                .ByStream(StreamFilter.Name(streamName))
                .ByBeforeOffset(afterStreamPosition)
                .OrderByDescending(x => x.EventStreamOffset)
                .Take(1)
                .ToListAsync(ct);

            result = Convert(commits, afterStreamPosition).ToList();
        }

        return result;
    }

    public async IAsyncEnumerable<StoredEvent> QueryAllReverseAsync(StreamFilter filter = default, DateTime timestamp = default, int take = int.MaxValue,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (take <= 0)
        {
            yield break;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        DateTime streamTime = timestamp;
        var query = await context.Set<EFEventCommit>()
            .ByStream(filter)
            .ByTimestamp(streamTime)
            .OrderByDescending(x => x.Position).ThenBy(x => x.EventStream)
            .Take(take)
            .ToListAsync(ct);

        var taken = 0;
        foreach (var commit in query)
        {
            foreach (var @event in commit.Filtered(EventsVersion.Empty).Reverse())
            {
                yield return @event;

                taken++;
                if (taken == take)
                {
                    yield break;
                }
            }
        }
    }

    public async IAsyncEnumerable<StoredEvent> QueryAllAsync(StreamFilter filter = default, StreamPosition position = default, int take = int.MaxValue,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (take <= 0 || position.IsEnd)
        {
            yield break;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        ParsedStreamPosition streamPosition = position;
        var query = context.Set<EFEventCommit>()
            .ByStream(filter)
            .ByPosition(streamPosition)
            .OrderBy(x => x.Position).ThenBy(x => x.EventStream)
            .Take(take);

        var taken = 0;
        await foreach (var commit in query.AsAsyncEnumerable().WithCancellation(ct))
        {
            foreach (var @event in commit.Filtered(streamPosition))
            {
                yield return @event;

                taken++;
                if (taken == take)
                {
                    yield break;
                }
            }
        }
    }

    private static List<StoredEvent> Convert(IEnumerable<EFEventCommit> commits, long position)
    {
        return commits.OrderBy(x => x.EventStreamOffset).ThenBy(x => x.Timestamp).SelectMany(x => x.Filtered(position)).ToList();
    }
}
