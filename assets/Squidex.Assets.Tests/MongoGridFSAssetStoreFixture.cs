// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Squidex.Assets.Mongo;
using Squidex.Hosting;
using Testcontainers.MongoDb;
using Xunit;

namespace Squidex.Assets;

public sealed class MongoGridFSAssetStoreFixture : IAsyncLifetime
{
    private readonly MongoDbContainer mongoDb =
        new MongoDbBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", "asset-postgres")
            .Build();

    private IServiceProvider services;

    public MongoGridFsAssetStore Store => services.GetRequiredService<MongoGridFsAssetStore>();

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

        services =
            new ServiceCollection()
                .AddSingleton<IMongoClient>(_ => new MongoClient(mongoDb.GetConnectionString()))
                .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"))
                .AddMongoAssetStore(c =>
                {
                    var mongoDatabase = c.GetRequiredService<IMongoDatabase>();

                    return new GridFSBucket<string>(mongoDatabase, new GridFSBucketOptions
                    {
                        BucketName = "TestBucket"
                    });
                })
                .BuildServiceProvider();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
