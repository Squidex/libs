// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Squidex.Events.Mongo;
using TestHelpers;
using TestHelpers.MongoDb;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events;

public sealed class MongoEventStoreReplicaFixture() : MongoReplicaSetFixture("eventstore-mongo-replicaset")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddMongoEventStore(TestUtils.Configuration, options =>
        {
            options.PollingInterval = TimeSpan.FromSeconds(0.1);
        });
    }
}

public class MongoEventStoreReplicaTests(MongoEventStoreReplicaFixture fixture)
    : EventStoreTests, IClassFixture<MongoEventStoreReplicaFixture>
{
    protected override Task<IEventStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IEventStore>();
        return Task.FromResult(store);
    }

    [Fact]
    public async Task Should_resume_change_stream_subscription_from_recent_position()
    {
        // The subscription clock is ahead, so that the events are older than the start of the change stream would be without a position.
        var options = fixture.Services.GetRequiredService<IOptions<MongoEventStoreOptions>>().Value;

        var sut = new MongoEventStore(
            fixture.MongoDatabase,
            Options.Create(new MongoEventStoreOptions
            {
                CollectionName = options.CollectionName,
                PollingInterval = options.PollingInterval,
                UseChangeStreams = true,
            }),
            new OffsetTimeProvider(TimeSpan.FromMinutes(2)));

        await sut.InitializeAsync(default);

        Assert.True(sut.CanUseChangeStreams);

        var streamName = $"test-{Guid.NewGuid()}";
        var streamFilter = StreamFilter.Name(streamName);

        // Multiple events per commit to verify that events before the position are not delivered again.
        await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, [CreateRecentEvent(0), CreateRecentEvent(1), CreateRecentEvent(2)]);
        await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, [CreateRecentEvent(3), CreateRecentEvent(4)]);

        var position = (await sut.QueryAllAsync(streamFilter).ToListAsync())[^1].EventPosition;

        await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Any, [CreateRecentEvent(5), CreateRecentEvent(6)]);

        var subscriber = new EventSubscriber();

        using (var subscription = sut.CreateSubscription(subscriber, streamFilter, position))
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            while (subscriber.LastEvents.Count < 2 && !cts.IsCancellationRequested)
            {
                await Task.Delay(100, default);
            }

            // Wait a little bit longer to detect duplicates.
            await Task.Delay(1000, default);
        }

        Assert.Equal(["Type5", "Type6"], subscriber.LastEvents.Select(x => x.Data.Type));
    }

    private static EventData CreateRecentEvent(int i)
    {
        var headers = new EnvelopeHeaders
        {
            [CoreHeaders.Timestamp] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
        };

        return new EventData($"Type{i}", headers, i.ToString(CultureInfo.InvariantCulture));
    }

    private sealed class OffsetTimeProvider(TimeSpan offset) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return base.GetUtcNow() + offset;
        }
    }
}
