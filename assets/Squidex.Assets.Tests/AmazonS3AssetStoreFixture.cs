// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public sealed class AmazonS3AssetStoreFixture
    {
        public AmazonS3AssetStore AssetStore { get; }

        public AmazonS3AssetStoreFixture()
        {
            var options = TestHelpers.Configuration.GetSection("amazonS3").Get<AmazonS3AssetOptions>();

            // From: https://console.aws.amazon.com/iam/home?region=eu-central-1#/users/s3?section=security_credentials
            AssetStore = new AmazonS3AssetStore(options);
            AssetStore.InitializeAsync(default).Wait();
        }
    }
}
