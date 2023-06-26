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
        var services =
            new ServiceCollection()
                .AddAzureBlobAssetStore(TestHelpers.Configuration)
                .BuildServiceProvider();

        AssetStore = services.GetRequiredService<AzureBlobAssetStore>();
        AssetStore.InitializeAsync(default).Wait();
    }
}
