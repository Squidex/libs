// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Assets;

public class MongoGridFsAssetStoreTests(MongoGridFSAssetStoreFixture fixture)
    : AssetStoreTests, IClassFixture<MongoGridFSAssetStoreFixture>
{
    public override Task<IAssetStore> CreateSutAsync()
    {
        return Task.FromResult<IAssetStore>(fixture.Store);
    }

    [Fact]
    public void Should_not_calculate_source_url()
    {
        var url = ((IAssetStore)fixture.Store).GeneratePublicUrl(FileName);

        Assert.Null(url);
    }
}
