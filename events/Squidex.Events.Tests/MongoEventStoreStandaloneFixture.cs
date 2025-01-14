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

public sealed class MongoEventStoreStandaloneFixture : IAsyncLifetime
{
    private readonly MongoDbContainer mongoDb =
        new MongoDbBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", "eventstore-mongo-standalone")
            .Build();

    private IServiceProvider services;

    public IEventStore Store => services.GetRequiredService<IEventStore>();

    public async Task DisposeAsync()
    {
        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }

        await mongoDb.StopAsync();
    }

    public async Task InitializeAsync()
    {
        await mongoDb.StartAsync();

        services = new ServiceCollection()
            .AddSingleton<IMongoClient>(_ => new MongoClient(mongoDb.GetConnectionString()))
            .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"))
            .AddMongoEventStore(TestHelpers.Configuration, options =>
            {
                options.PollingInterval = TimeSpan.FromSeconds(0.1);
            })
            .Services
            .BuildServiceProvider();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
