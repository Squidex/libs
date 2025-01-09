// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;
using Squidex.Hosting;
using Testcontainers.MongoDb;
using Xunit;

namespace Squidex.Messaging;

public class MongoMessagingDataStoreFixture : IAsyncLifetime
{
    private readonly MongoDbContainer mongoDB = new MongoDbBuilder().Build();
    private IServiceProvider services;

    public IMessagingDataStore Store => services.GetRequiredService<IMessagingDataStore>();

    public async Task DisposeAsync()
    {
        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }

        await mongoDB.StopAsync();
    }

    public async Task InitializeAsync()
    {
        await mongoDB.StartAsync();

        services = new ServiceCollection()
            .AddSingleton<IMongoClient>(_ => new MongoClient(mongoDB.GetConnectionString()))
            .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"))
            .AddMessaging()
            .AddMongoDataStore(new ConfigurationBuilder().Build())
            .Services
            .BuildServiceProvider();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
