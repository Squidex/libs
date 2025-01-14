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

    private IServiceProvider services;

    public IEventStore Store => services.GetRequiredService<IEventStore>();

    public async Task DisposeAsync()
    {
        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }

        await eventStore.StopAsync();
    }

    public async Task InitializeAsync()
    {
        await eventStore.StartAsync();

        services = new ServiceCollection()
            .AddSingleton(_ => EventStoreClientSettings.Create(eventStore.GetConnectionString()))
            .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"))
            .AddGetEventStore(TestHelpers.Configuration)
            .Services
            .BuildServiceProvider();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
