// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public sealed class GoogleCloudAssetStoreFixture
{
    public GoogleCloudAssetStore AssetStore { get; }

    public GoogleCloudAssetStoreFixture()
    {
        var services =
            new ServiceCollection()
                .AddGoogleCloudAssetStore(TestHelpers.Configuration)
                .BuildServiceProvider();

        AssetStore = services.GetRequiredService<GoogleCloudAssetStore>();
        AssetStore.InitializeAsync(default).Wait();
    }
}
