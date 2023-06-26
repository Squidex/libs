// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Squidex.Assets;

public sealed class MongoGridFSAssetStoreFixture : IDisposable
{
    private readonly IMongoClient mongoClient = new MongoClient(TestHelpers.Configuration["assetStore:mongoDB:connectionString"]);

    public MongoGridFsAssetStore AssetStore { get; }

    public MongoGridFSAssetStoreFixture()
    {
        var mongoDatabase = mongoClient.GetDatabase(TestHelpers.Configuration["assetStore:mongoDB:database"]);

        var gridFSBucket = new GridFSBucket<string>(mongoDatabase, new GridFSBucketOptions
        {
            BucketName = TestHelpers.Configuration["assetStore:mongoDB:bucketName"]
        });

        var services =
            new ServiceCollection()
                .AddMongoAssetStore(c => gridFSBucket)
                .BuildServiceProvider();

        AssetStore = services.GetRequiredService<MongoGridFsAssetStore>();
        AssetStore.InitializeAsync(default).Wait();
    }

    public void Dispose()
    {
        mongoClient.DropDatabase(TestHelpers.Configuration["assetStore:mongoDB:database"]);
    }
}
