﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using EventStore.Client;
using Microsoft.Extensions.Options;
using Squidex.Hosting;
using Squidex.Hosting.Configuration;
using ESStreamPosition = EventStore.Client.StreamPosition;

namespace Squidex.Events.GetEventStore;

public sealed class GetEventStore(
    EventStoreClientSettings settings,
    IOptions<GetEventStoreOptions> options)
    : IEventStore, IInitializable
{
    private readonly EventStoreClient client = new EventStoreClient(settings);
    private readonly EventStoreProjectionClient projectionClient = new EventStoreProjectionClient(settings, options);

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        try
        {
            await client.DeleteAsync(Guid.NewGuid().ToString(), StreamState.NoStream, cancellationToken: ct);
        }
        catch (WrongExpectedVersionException)
        {
            return;
        }
        catch (Exception ex)
        {
            var error = new ConfigurationError("GetEventStore cannot connect to event store.");

            throw new ConfigurationException(error, ex);
        }
    }

    public IEventSubscription CreateSubscription(IEventSubscriber<StoredEvent> subscriber, StreamFilter filter = default, StreamPosition position = default)
    {
        return new GetEventStoreSubscription(subscriber, client, projectionClient, options.Value.Prefix, filter, position);
    }

    public async IAsyncEnumerable<StoredEvent> QueryAllAsync(StreamFilter filter = default, StreamPosition position = default, int take = int.MaxValue,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (take <= 0 || position.ReadFromEnd)
        {
            yield break;
        }

        var streamName = await projectionClient.CreateProjectionAsync(filter, true, ct);
        var streamEvents = QueryAsync(streamName, position.ToPosition(false), take, ct);

        await foreach (var storedEvent in streamEvents.IgnoreNotFound(ct))
        {
            yield return storedEvent;
        }
    }

    public async IAsyncEnumerable<StoredEvent> QueryAllReverseAsync(StreamFilter filter = default, DateTime timestamp = default, int take = int.MaxValue,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (take <= 0)
        {
            yield break;
        }

        var streamName = await projectionClient.CreateProjectionAsync(filter, true, ct);
        var streamEvents = QueryReverseAsync(streamName, take, ct);

        var query = streamEvents
            .IgnoreNotFound(ct)
            .TakeWhile(x => x.Data.Headers.Timestamp() >= timestamp)
            .Take(take);

        await foreach (var storedEvent in query.WithCancellation(ct))
        {
            yield return storedEvent;
        }
    }

    public async Task<IReadOnlyList<StoredEvent>> QueryStreamAsync(string streamName, long afterStreamPosition = EventsVersion.Empty,
        CancellationToken ct = default)
    {
        var result = new List<StoredEvent>();

        var streamPath = GetStreamName(streamName);
        var streamEvents = QueryAsync(streamPath, afterStreamPosition.ToPositionBefore(), int.MaxValue, ct);

        await foreach (var storedEvent in streamEvents.IgnoreNotFound(ct))
        {
            result.Add(storedEvent);
        }

        return result.ToList();
    }

    private IAsyncEnumerable<StoredEvent> QueryAsync(string streamName, ESStreamPosition start, long count,
        CancellationToken ct = default)
    {
        var result = client.ReadStreamAsync(
            Direction.Forwards,
            streamName,
            start,
            count,
            true,
            cancellationToken: ct);

        return result.Select(x => Formatter.Read(x, options.Value.Prefix));
    }

    private IAsyncEnumerable<StoredEvent> QueryReverseAsync(string streamName, long count,
        CancellationToken ct = default)
    {
        var result = client.ReadStreamAsync(
            Direction.Backwards,
            streamName,
            ESStreamPosition.End,
            count,
            true,
            cancellationToken: ct);

        return result.Select(x => Formatter.Read(x, options.Value.Prefix));
    }

    public async Task DeleteStreamAsync(string streamName,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamName);

        await client.DeleteAsync(GetStreamName(streamName), StreamState.Any, cancellationToken: ct);
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

        try
        {
            var eventData = events.Select(Formatter.Write);

            streamName = GetStreamName(streamName);

            if (expectedVersion == -1)
            {
                await client.AppendToStreamAsync(streamName, StreamState.NoStream, eventData, cancellationToken: ct);
            }
            else if (expectedVersion < -1)
            {
                await client.AppendToStreamAsync(streamName, StreamState.Any, eventData, cancellationToken: ct);
            }
            else
            {
                await client.AppendToStreamAsync(streamName, expectedVersion.ToRevision(), eventData, cancellationToken: ct);
            }
        }
        catch (WrongExpectedVersionException ex)
        {
            throw new WrongEventVersionException(ex.ActualVersion ?? 0, expectedVersion);
        }
    }

    public async Task DeleteAsync(StreamFilter filter,
        CancellationToken ct = default)
    {
        var streamName = await projectionClient.CreateProjectionAsync(filter, true, ct);

        var events = client.ReadStreamAsync(Direction.Forwards, streamName, ESStreamPosition.Start, resolveLinkTos: true, cancellationToken: ct);
        if (await events.ReadState == ReadState.StreamNotFound)
        {
            return;
        }

        var deleted = new HashSet<string>();
        await foreach (var storedEvent in TaskAsyncEnumerableExtensions.WithCancellation(events, ct))
        {
            var streamToDelete = storedEvent.Event.EventStreamId;

            if (deleted.Add(streamToDelete))
            {
                await client.DeleteAsync(streamToDelete, StreamState.Any, cancellationToken: ct);
            }
        }
    }

    private string GetStreamName(string streamName)
    {
        return $"{options.Value.Prefix}-{streamName}";
    }
}
