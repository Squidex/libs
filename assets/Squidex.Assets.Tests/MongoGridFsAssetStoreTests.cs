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
public class MongoGridFsAssetStoreTests(MongoGridFSAssetStoreFixture fixture) : AssetStoreTests<MongoGridFsAssetStore>, IClassFixture<MongoGridFSAssetStoreFixture>
{
    public MongoGridFSAssetStoreFixture _ { get; } = fixture;

    public override MongoGridFsAssetStore CreateStore()
    {
        return _.AssetStore;
    }

    [Fact]
    public void Should_not_calculate_source_url()
    {
        var url = ((IAssetStore)Sut).GeneratePublicUrl(FileName);

        Assert.Null(url);
    }
}
