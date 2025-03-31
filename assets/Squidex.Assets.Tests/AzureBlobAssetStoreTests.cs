// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Assets;

[Trait("Category", "Dependencies")]
public class AzureBlobAssetStoreTests(AzureBlobAssetStoreFixture fixture)
    : AssetStoreTests, IClassFixture<AzureBlobAssetStoreFixture>
{
    public override Task<IAssetStore> CreateSutAsync()
    {
        return Task.FromResult<IAssetStore>(fixture.Store);
    }

    [Fact]
    public void Should_calculate_source_url()
    {
        var url = fixture.Store.GeneratePublicUrl(FileName);

        Assert.Equal($"http://127.0.0.1:10000/devstoreaccount1/squidex-test-container/{FileName}", url);
    }
}
