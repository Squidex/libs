// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public sealed class GoogleCloudAssetStoreFixture
    {
        public GoogleCloudAssetStore AssetStore { get; }

        public GoogleCloudAssetStoreFixture()
        {
            var options = TestHelpers.Configuration.GetSection("googleCloud").Get<GoogleCloudAssetOptions>();

            AssetStore = new GoogleCloudAssetStore(options);
            AssetStore.InitializeAsync(default).Wait();
        }
    }
}
