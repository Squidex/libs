// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.S3;

namespace Squidex.Assets;

public sealed class CloudflareR2Fixture : IAsyncLifetime
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
    }

    public async Task InitializeAsync()
    {
        await Store.InitializeAsync(default);
    }

    public async Task DisposeAsync()
    {
        await Store.ReleaseAsync(default);
    }
}
