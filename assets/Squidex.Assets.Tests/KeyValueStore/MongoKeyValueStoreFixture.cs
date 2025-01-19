// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;
using MongoDB.Driver;
using Squidex.Assets.Mongo;
using Squidex.Hosting;
using Testcontainers.MongoDb;
using Xunit;

namespace Squidex.Assets.KeyValueStore;

public class MongoKeyValueStoreFixture : IAsyncLifetime
{
    private readonly MongoDbContainer mongoDb =
        new MongoDbBuilder()
            .WithReuse(Debugger.IsAttached)
            .WithLabel("reuse-id", "asset-kvp-mongo")
            .Build();

    public IServiceProvider Services { get; private set; }

    public MongoAssetKeyValueStore<TestValue> Store => Services.GetRequiredService<MongoAssetKeyValueStore<TestValue>>();

    public async Task InitializeAsync()
    {
        await mongoDb.StartAsync();

        Services =
            new ServiceCollection()
                .AddSingleton<IMongoClient>(_ => new MongoClient(mongoDb.GetConnectionString()))
                .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"))
                .AddMongoAssetKeyValueStore()
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
