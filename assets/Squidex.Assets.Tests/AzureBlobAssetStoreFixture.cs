// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public sealed class AzureBlobAssetStoreFixture
{
    public AzureBlobAssetStore AssetStore { get; }

    public AzureBlobAssetStoreFixture()
    {
        var options = TestHelpers.Configuration.GetSection("azureBlob").Get<AzureBlobAssetOptions>();

        AssetStore = new AzureBlobAssetStore(options);
        AssetStore.InitializeAsync(default).Wait();
    }
}
