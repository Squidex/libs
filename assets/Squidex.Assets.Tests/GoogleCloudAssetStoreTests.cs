// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Assets;

[Trait("Category", "Dependencies")]
public class GoogleCloudAssetStoreTests(GoogleCloudAssetStoreFixture fixture)
    : AssetStoreTests, IClassFixture<GoogleCloudAssetStoreFixture>
{
    public override Task<IAssetStore> CreateSutAsync()
    {
        return Task.FromResult<IAssetStore>(fixture.Store);
    }

    [Fact]
    public void Should_calculate_source_url()
    {
        var url = ((IAssetStore)fixture.Store).GeneratePublicUrl(FileName);

        Assert.Null(url);
    }
}
