// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public sealed class AmazonS3AssetStoreFixture
{
    public AmazonS3AssetStore AssetStore { get; }

    public AmazonS3AssetStoreFixture()
    {
        // From: https://console.aws.amazon.com/iam/home?region=eu-central-1#/users/s3?section=security_credentials
        var services =
            new ServiceCollection()
                .AddAmazonS3AssetStore(TestHelpers.Configuration)
                .BuildServiceProvider();

        AssetStore = services.GetRequiredService<AmazonS3AssetStore>();
        AssetStore.InitializeAsync(default).Wait();
    }
}
