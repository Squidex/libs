// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public sealed class CloudflareR2Fixture
{
    public AmazonS3AssetStore AssetStore { get; }

    public CloudflareR2Fixture()
    {
        // https://dash.cloudflare.com/{PROJECT_ID}/r2/overview/api-tokens
        var options = TestHelpers.Configuration.GetSection("r2").Get<AmazonS3AssetOptions>()!;

        AssetStore = new AmazonS3AssetStore(options);
        AssetStore.InitializeAsync(default).Wait();
    }
}
