// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace Squidex.Assets;

[Trait("Category", "Dependencies")]
public class CloudflareR2Tests : AssetStoreTests<AmazonS3AssetStore>, IClassFixture<CloudflareR2Fixture>
{
    public CloudflareR2Fixture _ { get; }

    public CloudflareR2Tests(CloudflareR2Fixture fixture)
    {
        _ = fixture;
    }

    public override AmazonS3AssetStore CreateStore()
    {
        return _.AssetStore;
    }

    [Fact]
    public async Task Should_throw_exception_for_invalid_config()
    {
        var sut = new AmazonS3AssetStore(new AmazonS3AssetOptions
        {
            AccessKey = "invalid",
            Bucket = "invalid",
            BucketFolder = null!,
            ForcePathStyle = false,
            RegionName = "invalid",
            SecretKey = "invalid",
            ServiceUrl = null!
        });

        await Assert.ThrowsAsync<AssetStoreException>(() => sut.InitializeAsync(default));
    }

    [Fact]
    public void Should_calculate_source_url()
    {
        var url = ((IAssetStore)Sut).GeneratePublicUrl(FileName);

        Assert.Null(url);
    }
}
