// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public class MemoryAssetStoreTests : AssetStoreTests
{
    public override Task<IAssetStore> CreateSutAsync()
    {
        return Task.FromResult<IAssetStore>(new MemoryAssetStore());
    }

    [Fact]
    public async Task Should_not_calculate_source_url()
    {
        var sut = await CreateSutAsync();

        var url = sut.GeneratePublicUrl(FileName);

        Assert.Null(url);
    }
}
