﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;

#pragma warning disable MA0040 // Forward the CancellationToken parameter to methods that take one

namespace Squidex.Events;

public abstract class EventStoreTests
{
    private readonly CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    private readonly CancellationToken ct;
    private StreamPosition subscriptionPosition;

    public sealed class EventSubscriber : IEventSubscriber<StoredEvent>
    {
        public List<StoredEvent> LastEvents { get; } = [];

        public StreamPosition LastPosition { get; set; }

        public ValueTask OnErrorAsync(IEventSubscription subscription, Exception exception)
        {
            throw exception;
        }

        public ValueTask OnNextAsync(IEventSubscription subscription, StoredEvent @event)
        {
            LastPosition = @event.EventPosition;
            LastEvents.Add(@event);
            return default;
        }
    }

    protected abstract Task<IEventStore> CreateSutAsync();

    protected EventStoreTests()
    {
        ct = cts.Token;
    }

    [Fact]
    public async Task Should_throw_exception_for_version_mismatch()
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";

        var commit = new[]
        {
            CreateEventData(1),
            CreateEventData(2),
        };

        await Assert.ThrowsAsync<WrongEventVersionException>(() => sut.AppendAsync(Guid.NewGuid(), streamName, 0, commit));
    }

    [Fact]
    public async Task Should_throw_exception_for_version_mismatch_and_update()
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";

        var commit = new[]
        {
            CreateEventData(1),
            CreateEventData(2),
        };

        await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, commit);

        await Assert.ThrowsAsync<WrongEventVersionException>(() => sut.AppendAsync(Guid.NewGuid(), streamName, 0, commit));
    }

    [Fact]
    public async Task Should_append_events()
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Name(streamName);

        var commit1 = new[]
        {
            CreateEventData(1),
            CreateEventData(2),
        };

        var commit2 = new[]
        {
            CreateEventData(1),
            CreateEventData(2),
        };

        await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, commit1, ct);
        await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, commit2, ct);

        var readEvents1 = await sut.QueryStreamAsync(streamName, ct: ct);
        var readEvents2 = await sut.QueryAllAsync(streamFilter, ct: ct).ToListAsync();

        var expected = new[]
        {
            new StoredEvent(streamName, "Position", 0, commit1[0]),
            new StoredEvent(streamName, "Position", 1, commit1[1]),
            new StoredEvent(streamName, "Position", 2, commit2[0]),
            new StoredEvent(streamName, "Position", 3, commit2[1]),
        };

        ShouldBeEquivalentTo(readEvents1, expected);
        ShouldBeEquivalentTo(readEvents2, expected);
    }

    [Fact]
    public async Task Should_append_events_unsafe()
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Name(streamName);

        var commit1 = Enumerable.Range(0, 100).Select(x => CreateEventData(1)).ToArray();

        await sut.AppendUnsafeAsync(
        [
            new EventCommit(Guid.NewGuid(), streamName, -1, commit1),
        ], ct);

        var readEvents1 = await sut.QueryStreamAsync(streamName, ct: ct);
        var readEvents2 = await sut.QueryAllAsync(streamFilter, ct: ct).ToListAsync();

        var expected = commit1.Select((x, i) => new StoredEvent(streamName, "Position", i, x)).ToArray();

        ShouldBeEquivalentTo(readEvents1, expected);
        ShouldBeEquivalentTo(readEvents2, expected);
    }

    [Fact]
    public async Task Should_return_no_result_if_queried_from_end()
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Name(streamName);

        var commit1 = new[]
        {
            CreateEventData(1),
            CreateEventData(2),
        };

        var commit2 = new[]
        {
            CreateEventData(1),
            CreateEventData(2),
        };

        await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, commit1, ct);
        await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, commit2, ct);

        var readEvents = await sut.QueryAllAsync(streamFilter, StreamPosition.End, ct: ct).ToListAsync();

        Assert.Empty(readEvents);
    }

    [Fact]
    public async Task Should_subscribe_to_events()
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Name(streamName);

        var commit1 = new[]
        {
            CreateEventData(1),
            CreateEventData(2),
        };

        var readEvents = await QueryWithSubscriptionAsync(sut, streamFilter, 1, async () =>
        {
            await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, commit1, ct);
        });

        var expected = new[]
        {
            new StoredEvent(streamName, "Position", 0, commit1[0]),
            new StoredEvent(streamName, "Position", 1, commit1[1]),
        };

        ShouldBeEquivalentTo(readEvents, expected);
    }

    [Fact]
    public async Task Should_subscribe_to_events_after_commit()
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Name(streamName);

        var commit1 = new[]
        {
            CreateEventData(1),
            CreateEventData(2),
        };

        await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, commit1, ct);

        var readEvents = await QueryWithSubscriptionAsync(sut, streamFilter, 1);

        var expected = new[]
        {
            new StoredEvent(streamName, "Position", 0, commit1[0]),
            new StoredEvent(streamName, "Position", 1, commit1[1]),
        };

        ShouldBeEquivalentTo(readEvents, expected);
    }

    [Fact]
    public async Task Should_subscribe_to_next_events()
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Name(streamName);

        var commit1 = new[]
        {
            CreateEventData(1),
            CreateEventData(2),
        };

        // Append and read in parallel.
        await QueryWithSubscriptionAsync(sut, streamFilter, 1, async () =>
        {
            await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, commit1, ct);
        });

        var commit2 = new[]
        {
            CreateEventData(1),
            CreateEventData(2),
        };

        // Append and read in parallel.
        var readEventsFromPosition = await QueryWithSubscriptionAsync(sut, streamFilter, 1, async () =>
        {
            await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, commit2, ct);
        });

        var expectedFromPosition = new[]
        {
            new StoredEvent(streamName, "Position", 2, commit2[0]),
            new StoredEvent(streamName, "Position", 3, commit2[1]),
        };

        var readEventsFromBeginning = await QueryWithSubscriptionAsync(sut, streamFilter, 1, fromBeginning: true);

        var expectedFromBeginning = new[]
        {
            new StoredEvent(streamName, "Position", 0, commit1[0]),
            new StoredEvent(streamName, "Position", 1, commit1[1]),
            new StoredEvent(streamName, "Position", 2, commit2[0]),
            new StoredEvent(streamName, "Position", 3, commit2[1]),
        };

        ShouldBeEquivalentTo(readEventsFromPosition?.TakeLast(2), expectedFromPosition);
        ShouldBeEquivalentTo(readEventsFromBeginning?.TakeLast(4), expectedFromBeginning);
    }

    [Fact]
    public async Task Should_subscribe_with_parallel_writes()
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Prefix(streamName);

        var numTasks = 50;
        var numEvents = 20;

        // We need to be able to run the test fast.
        var expectedEvents = (int)(numTasks * numEvents * 0.9);

        // Append and read in parallel.
        var readEvents = await QueryWithSubscriptionAsync(sut, streamFilter, expectedEvents, async () =>
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, numTasks), async (i, ct) =>
            {
                var fullStreamName = $"{streamName}-{Guid.NewGuid()}";

                for (var j = 0; j < numEvents; j++)
                {
                    var commit = new[]
                    {
                        CreateEventData(i * j),
                    };

                    await sut.AppendAsync(Guid.NewGuid(), fullStreamName, EventsVersion.Any, commit, ct);
                }
            });
        });

        Assert.True(readEvents?.Count >= expectedEvents);
    }

    [Fact]
    public async Task Should_read_multiple_streams()
    {
        var sut = await CreateSutAsync();

        var streamName1 = $"test-{Guid.NewGuid()}";
        var streamName2 = $"test-{Guid.NewGuid()}";

        var stream1Commit = new[]
        {
            CreateEventData(1),
            CreateEventData(2),
        };

        var stream2Commit = new[]
        {
            CreateEventData(3),
            CreateEventData(4),
        };

        await sut.AppendAsync(Guid.NewGuid(), streamName1, EventsVersion.Any, stream1Commit, ct);
        await sut.AppendAsync(Guid.NewGuid(), streamName2, EventsVersion.Any, stream2Commit, ct);

        var readEvents = await sut.QueryAllAsync(StreamFilter.Name(streamName1, streamName2), ct: ct).ToListAsync();

        var expected1 = new[]
        {
            new StoredEvent(streamName1, "Position", 0, stream1Commit[0]),
            new StoredEvent(streamName1, "Position", 1, stream1Commit[1]),
        };

        var expected2 = new[]
        {
            new StoredEvent(streamName2, "Position", 0, stream2Commit[0]),
            new StoredEvent(streamName2, "Position", 1, stream2Commit[1]),
        };

        ShouldBeEquivalentTo(readEvents.Where(x => x.StreamName == streamName1), expected1);
        ShouldBeEquivalentTo(readEvents.Where(x => x.StreamName == streamName2), expected2);
    }

    [Theory]
    [InlineData(1, 30)]
    [InlineData(5, 30)]
    [InlineData(5, 300)]
    [InlineData(5, 3000)]
    public async Task Should_query_events_from_offset(int commits, int count)
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Name(streamName);

        var eventsWritten = await AppendEventsAsync(sut, streamName, count, commits);

        var readEvents0 = await sut.QueryStreamAsync(streamName, ct: ct);
        var readEvents1 = await sut.QueryStreamAsync(streamName, count - 2, ct);
        var readEvents2 = await sut.QueryAllAsync(streamFilter, readEvents0[^2].EventPosition, ct: ct).ToListAsync();

        var expected = new[]
        {
            new StoredEvent(streamName, "Position", count - 1, eventsWritten[^1]),
        };

        ShouldBeEquivalentTo(readEvents1, expected);
        ShouldBeEquivalentTo(readEvents2, expected);
    }

    [Theory]
    [InlineData(5, 30)]
    public async Task Should_query_events_from_position_one_by_one(int commits, int count)
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Name(streamName);

        var eventsWritten = await AppendEventsAsync(sut, streamName, count, commits);
        var eventsRead = 0;
        var lastPosition = string.Empty;

        while (true)
        {
            var read = await sut.QueryAllAsync(streamFilter, lastPosition, 1, ct).ToListAsync();
            eventsRead += read.Count;

            if (read.Count == 0)
            {
                break;
            }

            lastPosition = read[^1].EventPosition;
        }

        Assert.Equal(eventsWritten.Count, eventsRead);
    }

    [Theory]
    [InlineData(5, 30)]
    [InlineData(5, 300)]
    [InlineData(5, 3000)]
    public async Task Should_query_all_reverse_by_names(int commits, int count)
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Name(streamName, "invalid");

        var eventsWritten = await AppendEventsAsync(sut, streamName, count, commits);
        var eventsStored = eventsWritten.Select((x, i) => new StoredEvent(streamName, "Position", i, x)).ToArray();

        for (var take = 0; take < count; take += count / 10)
        {
            var eventsExpected = eventsStored.Reverse().Take(take).ToArray();
            var eventsQueried = await sut.QueryAllReverseAsync(streamFilter, default, take, ct).ToArrayAsync();

            ShouldBeEquivalentTo(eventsQueried, eventsExpected);
        }
    }

    [Theory]
    [InlineData(5, 30)]
    [InlineData(5, 300)]
    [InlineData(5, 3000)]
    public async Task Should_query_all_reverse_by_prefix(int commits, int count)
    {
        var sut = await CreateSutAsync();

        var randomPart = Guid.NewGuid();
        var streamName = $"test-{randomPart}-suffix";
        var streamFilter = StreamFilter.Prefix(streamName[..^7], "invalid");

        var eventsWritten = await AppendEventsAsync(sut, streamName, count, commits);
        var eventsStored = eventsWritten.Select((x, i) => new StoredEvent(streamName, "Position", i, x)).ToArray();

        for (var take = 0; take < count; take += count / 10)
        {
            var eventsExpected = eventsStored.Reverse().Take(take).ToArray();
            var eventsQueried = await sut.QueryAllReverseAsync(streamFilter, default, take, ct).ToArrayAsync();

            ShouldBeEquivalentTo(eventsQueried, eventsExpected);
        }
    }

    [Theory]
    [InlineData(5, 30)]
    [InlineData(5, 300)]
    [InlineData(5, 3000)]
    public async Task Should_query_all_reverse_by_wildcard_prefix(int commits, int count)
    {
        var sut = await CreateSutAsync();

        var randomPart = Guid.NewGuid();
        var streamName = $"test-{randomPart}-suffix";
        var streamFilter = StreamFilter.Prefix($"%-{randomPart}", "invalid");

        var eventsWritten = await AppendEventsAsync(sut, streamName, count, commits);
        var eventsStored = eventsWritten.Select((x, i) => new StoredEvent(streamName, "Position", i, x)).ToArray();

        for (var take = 0; take < count; take += count / 10)
        {
            var eventsExpected = eventsStored.Reverse().Take(take).ToArray();
            var eventsQueried = await sut.QueryAllReverseAsync(streamFilter, default, take, ct).ToArrayAsync();

            ShouldBeEquivalentTo(eventsQueried, eventsExpected);
        }
    }

    [Theory]
    [InlineData(5, 30)]
    [InlineData(5, 300)]
    [InlineData(5, 3000)]
    public async Task Should_read_all_reverse(int commits, int count)
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}-suffix";
        var streamFilter = default(StreamFilter);

        await AppendEventsAsync(sut, streamName, count, commits);

        for (var take = 0; take < count; take += count / 10)
        {
            var eventsQueried = await sut.QueryAllReverseAsync(streamFilter, default, take, ct).ToArrayAsync();

            Assert.Equal(take, eventsQueried.Length);
        }
    }

    [Fact]
    public async Task Should_delete_by_filter()
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Prefix($"{streamName[..10]}");

        await AppendEventsAsync(sut, streamName, 2, 1);

        IReadOnlyList<StoredEvent>? readEvents = null;

        for (var i = 0; i < 5; i++)
        {
            await sut.DeleteAsync(streamFilter);

            readEvents = await sut.QueryStreamAsync(streamName, ct: ct);
            if (readEvents.Count == 0)
            {
                break;
            }

            // Get event store needs a little bit of time for the projections.
            await Task.Delay(1000);
        }

        Assert.Empty(readEvents!);
    }

    [Fact]
    public async Task Should_delete_by_name()
    {
        var sut = await CreateSutAsync();

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Name(streamName);

        await AppendEventsAsync(sut, streamName, 2, 1);

        IReadOnlyList<StoredEvent>? readEvents = null;

        for (var i = 0; i < 5; i++)
        {
            await sut.DeleteAsync(streamFilter, ct);

            readEvents = await sut.QueryStreamAsync(streamName, ct: ct);
            if (readEvents.Count == 0)
            {
                break;
            }

            // Get event store needs a little bit of time for the projections.
            await Task.Delay(1000);
        }

        Assert.Empty(readEvents!);
    }

    private static EventData CreateEventData(int i)
    {
        var headers = new EnvelopeHeaders
        {
            ["EventId"] = Guid.NewGuid().ToString(),
        };

        return new EventData($"Type{i}", headers, i.ToString(CultureInfo.InvariantCulture));
    }

    private async Task<IReadOnlyList<StoredEvent>> QueryWithSubscriptionAsync(
        IEventStore sut,
        StreamFilter streamFilter,
        int expectedCount,
        Func<Task>? subscriptionRunning = null,
        bool fromBeginning = false)
    {
        var subscriber = new EventSubscriber();

        IEventSubscription? subscription = null;
        try
        {
            subscription = sut.CreateSubscription(subscriber, streamFilter, fromBeginning ? default : subscriptionPosition);

            if (subscriptionRunning != null)
            {
                await subscriptionRunning();
            }

            using (var cts2 = new CancellationTokenSource(30_000))
            {
                while (!cts2.IsCancellationRequested)
                {
                    subscription.WakeUp();

                    await Task.Delay(2000, cts2.Token);

                    if (subscriber.LastEvents.Count >= expectedCount)
                    {
                        subscriptionPosition = subscriber.LastPosition;
                        return subscriber.LastEvents;
                    }
                }
            }

            return subscriber.LastEvents;
        }
        catch (OperationCanceledException)
        {
            return subscriber.LastEvents;
        }
        finally
        {
            subscription?.Dispose();
        }
    }

    private async Task<List<EventData>> AppendEventsAsync(IEventStore sut, string streamName, int count, int commits = 1)
    {
        var events = new List<EventData>();

        for (var i = 0; i < count; i++)
        {
            events.Add(CreateEventData(i));
        }

        for (var i = 0; i < events.Count / commits; i++)
        {
            var commit = events.Skip(i * commits).Take(commits).ToArray();

            await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, commit, ct);
        }

        return events;
    }

    private static void ShouldBeEquivalentTo(IEnumerable<StoredEvent>? actual, params StoredEvent[] expected)
    {
        actual.Should().BeEquivalentTo(expected, opts => opts.ComparingByMembers<StoredEvent>().Excluding(x => x.EventPosition));
    }
}
