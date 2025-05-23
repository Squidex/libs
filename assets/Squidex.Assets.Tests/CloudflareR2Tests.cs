// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using Squidex.Assets.S3;

namespace Squidex.Assets;

[Trait("Category", "Dependencies")]
public class CloudflareR2Tests(CloudflareR2Fixture fixture)
    : AssetStoreTests, IClassFixture<CloudflareR2Fixture>
{
    public override Task<IAssetStore> CreateSutAsync()
    {
        return Task.FromResult<IAssetStore>(fixture.Store);
    }

    [Fact]
    public async Task Should_throw_exception_for_invalid_config()
    {
        var sut = new AmazonS3AssetStore(Options.Create(new AmazonS3AssetOptions
        {
            AccessKey = "invalid",
            Bucket = "invalid",
            BucketFolder = null!,
            ForcePathStyle = false,
            RegionName = "invalid",
            SecretKey = "invalid",
            ServiceUrl = null!,
        }));

        await Assert.ThrowsAsync<AssetStoreException>(() => sut.InitializeAsync(default));
    }

    [Fact]
    public void Should_calculate_source_url()
    {
        var url = ((IAssetStore)fixture.Store).GeneratePublicUrl(FileName);

        Assert.Null(url);
    }
}
