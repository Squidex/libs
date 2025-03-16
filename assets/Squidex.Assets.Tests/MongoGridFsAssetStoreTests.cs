// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using TestHelpers.MongoDb;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Assets;

public sealed class MongoGridFSAssetStoreFixture() : MongoFixture("assets-mongo")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddMongoAssetStore(c =>
        {
            var mongoDatabase = c.GetRequiredService<IMongoDatabase>();

            return new GridFSBucket<string>(mongoDatabase, new GridFSBucketOptions
            {
                BucketName = "TestBucket",
            });
        });
    }
}

public class MongoGridFsAssetStoreTests(MongoGridFSAssetStoreFixture fixture)
    : AssetStoreTests, IClassFixture<MongoGridFSAssetStoreFixture>
{
    public override Task<IAssetStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IAssetStore>();
        return Task.FromResult<IAssetStore>(store);
    }

    [Fact]
    public void Should_not_calculate_source_url()
    {
        var url = fixture.Services.GetRequiredService<IAssetStore>().GeneratePublicUrl(FileName);

        Assert.Null(url);
    }
}
