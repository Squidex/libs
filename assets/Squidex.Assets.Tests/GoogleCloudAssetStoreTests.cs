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
public class GoogleCloudAssetStoreTests(GoogleCloudAssetStoreFixture fixture) : AssetStoreTests<GoogleCloudAssetStore>, IClassFixture<GoogleCloudAssetStoreFixture>
{
    public GoogleCloudAssetStoreFixture _ { get; } = fixture;

    public override GoogleCloudAssetStore CreateStore()
    {
        return _.AssetStore;
    }

    [Fact]
    public void Should_calculate_source_url()
    {
        var url = ((IAssetStore)Sut).GeneratePublicUrl(FileName);

        Assert.Null(url);
    }
}
