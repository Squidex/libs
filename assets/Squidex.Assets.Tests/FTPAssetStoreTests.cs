﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace Squidex.Assets;

[Trait("Category", "Dependencies")]
public class FTPAssetStoreTests(FTPAssetStoreFixture fixture) : AssetStoreTests<FTPAssetStore>, IClassFixture<FTPAssetStoreFixture>
{
    public FTPAssetStoreFixture _ { get; } = fixture;

    protected override bool CanUploadStreamsWithoutLength => false;

    protected override bool CanDeleteAssetsWithPrefix => false;

    public override FTPAssetStore CreateStore()
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
