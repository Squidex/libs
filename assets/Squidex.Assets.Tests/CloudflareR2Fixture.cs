// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public sealed class CloudflareR2Fixture
{
    public AmazonS3AssetStore Store { get; }

    public CloudflareR2Fixture()
    {
        // From: https://dash.cloudflare.com/{PROJECT_ID}/r2/overview/api-tokens
        var services =
            new ServiceCollection()
                .AddAmazonS3AssetStore(TestHelpers.Configuration, null, "assetStore:r2")
                .BuildServiceProvider();

        Store = services.GetRequiredService<AmazonS3AssetStore>();
        Store.InitializeAsync(default).Wait();
    }
}
