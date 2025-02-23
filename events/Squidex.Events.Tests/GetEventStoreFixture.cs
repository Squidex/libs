// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squidex.Events.GetEventStore;
using Squidex.Hosting;
using Testcontainers.EventStoreDb;
using Xunit;

namespace Squidex.Events;

public sealed class GetEventStoreFixture : IAsyncLifetime
{
    private readonly EventStoreDbContainer eventStore =
        new EventStoreDbBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", "eventstore-geteventstore")
            .WithEnvironment("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", "true")
            .Build();

    public IServiceProvider Services { get; private set; }

    public IEventStore Store => Services.GetRequiredService<IEventStore>();

    public async Task InitializeAsync()
    {
        await eventStore.StartAsync();

        Services = new ServiceCollection()
            .AddSingleton(_ => EventStoreClientSettings.Create(eventStore.GetConnectionString()))
            .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"))
            .AddGetEventStore(TestHelpers.Configuration)
            .Services
            .BuildServiceProvider();

        foreach (var service in Services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }

    public async Task DisposeAsync()
    {
        foreach (var service in Services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }

        await eventStore.StopAsync();
    }
}
