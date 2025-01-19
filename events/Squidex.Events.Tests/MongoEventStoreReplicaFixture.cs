// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squidex.Hosting;
using Testcontainers.MongoDb;
using Xunit;

namespace Squidex.Events;

public sealed class MongoEventStoreReplicaFixture : IAsyncLifetime
{
    private readonly MongoDbContainer mongoDb =
        new MongoDbBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", "eventstore-mongo-replica")
            .WithReplicaSet()
            .Build();

    public IServiceProvider Services { get; private set; }

    public IEventStore Store => Services.GetRequiredService<IEventStore>();

    public async Task InitializeAsync()
    {
        await mongoDb.StartAsync();

        Services = new ServiceCollection()
            .AddSingleton<IMongoClient>(_ => new MongoClient(mongoDb.GetConnectionString()))
            .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"))
            .AddMongoEventStore(TestHelpers.Configuration, options =>
            {
                options.PollingInterval = TimeSpan.FromSeconds(0.1);
            })
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

        await mongoDb.StopAsync();
    }
}
