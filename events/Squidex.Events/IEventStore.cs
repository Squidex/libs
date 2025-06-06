﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events;

public interface IEventStore
{
    Task<IReadOnlyList<StoredEvent>> QueryStreamAsync(string streamName, long afterStreamPosition = EventsVersion.Empty,
        CancellationToken ct = default);

    IAsyncEnumerable<StoredEvent> QueryAllReverseAsync(StreamFilter filter = default, DateTime timestamp = default, int take = int.MaxValue,
        CancellationToken ct = default);

    IAsyncEnumerable<StoredEvent> QueryAllAsync(StreamFilter filter = default, StreamPosition position = default, int take = int.MaxValue,
        CancellationToken ct = default);

    Task AppendAsync(Guid commitId, string streamName, long expectedVersion, ICollection<EventData> events,
        CancellationToken ct = default);

    Task DeleteAsync(StreamFilter filter,
        CancellationToken ct = default);

    IEventSubscription CreateSubscription(IEventSubscriber<StoredEvent> eventSubscriber, StreamFilter filter = default, StreamPosition position = default);

    async Task AppendUnsafeAsync(IEnumerable<EventCommit> commits,
        CancellationToken ct = default)
    {
        foreach (var commit in commits)
        {
            await AppendAsync(commit.Id, commit.StreamName, commit.Offset, commit.Events, ct);
        }
    }
}
