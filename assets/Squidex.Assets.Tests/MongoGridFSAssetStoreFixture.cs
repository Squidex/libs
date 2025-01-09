// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Squidex.Hosting;
using Testcontainers.MongoDb;
using Xunit;

namespace Squidex.Assets;

public sealed class MongoGridFSAssetStoreFixture : IAsyncLifetime
{
    private readonly MongoDbContainer mongoDB = new MongoDbBuilder().Build();
    private IServiceProvider services;

    public MongoGridFsAssetStore Store => services.GetRequiredService<MongoGridFsAssetStore>();

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

        var mongoClient = new MongoClient(mongoDB.GetConnectionString());
        var mongoDatabase = mongoClient.GetDatabase("Test");
        var gridFSBucket = new GridFSBucket<string>(mongoDatabase, new GridFSBucketOptions
        {
            BucketName = "TestBucket"
        });

        services =
            new ServiceCollection()
                .AddMongoAssetStore(c => gridFSBucket)
                .BuildServiceProvider();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
