// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Squidex.Events.Mongo;
using TestHelpers;
using TestHelpers.MongoDb;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events;

public sealed class MongoEventStoreFerretDbFixture() : MongoFerretFixture("eventstore-mongo-ferretdb")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddMongoEventStore(TestUtils.Configuration, options =>
        {
            options.PollingInterval = TimeSpan.FromSeconds(0.1);
            options.Derivate = MongoDerivate.FerretDB;
        });
    }
}

public class MongoEventStoreFerretDbTests(MongoEventStoreFerretDbFixture fixture)
    : EventStoreTests, IClassFixture<MongoEventStoreFerretDbFixture>
{
    protected override Task<IEventStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IEventStore>();
        return Task.FromResult(store);
    }

    [Fact]
    public async Task Should_only_delete_own_commits_without_position_if_append_fails()
    {
        var sut = (MongoEventStore)await CreateSutAsync();

        // Commit of another writer, that has been inserted, but has not received a global position yet.
        var otherCommit = new MongoEventCommit
        {
            Id = Guid.NewGuid(),
            Events = [MongoEvent.FromEventData(CreateEventData(0))],
            EventsCount = 1,
            EventStream = $"test-{Guid.NewGuid()}",
            EventStreamOffset = EventsVersion.Empty,
            Timestamp = new BsonTimestamp(0),
        };

        await sut.TypedCollection.InsertOneAsync(otherCommit);

        var streamName = $"test-{Guid.NewGuid()}";

        var commit1 = new EventCommit(Guid.NewGuid(), streamName, EventsVersion.Empty, [CreateEventData(1)]);
        var commit2 = new EventCommit(Guid.NewGuid(), streamName, EventsVersion.Empty, [CreateEventData(2)]);

        // The second commit conflicts with the first one.
        await Assert.ThrowsAnyAsync<MongoException>(() => sut.AppendUnsafeAsync([commit1, commit2]));

        var ids = await sut.TypedCollection.Find(x => x.Id == otherCommit.Id || x.Id == commit1.Id).Project(x => x.Id).ToListAsync();

        Assert.Equal([otherCommit.Id], ids);

        await sut.TypedCollection.DeleteOneAsync(x => x.Id == otherCommit.Id);
    }
}
