// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squidex.AI.Implementation;
using Squidex.AI.Mongo;
using Squidex.Hosting;
using Testcontainers.MongoDb;
using Xunit;

namespace Squidex.AI;

public sealed class MongoChatStoreFixture : IAsyncLifetime
{
    private readonly MongoDbContainer mongoDb = new MongoDbBuilder().Build();
    private IServiceProvider services;

    public MongoChatStore Store => services.GetRequiredService<MongoChatStore>();

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
            .AddMongoChatStore(new ConfigurationBuilder().Build())
            .BuildServiceProvider();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
