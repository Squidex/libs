// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.S3;
using Squidex.Hosting;

namespace Squidex.Assets;

public sealed class AmazonS3AssetStoreFixture : IAsyncLifetime
{
    public IServiceProvider Services { get; private set; }

    public AmazonS3AssetStore Store => Services.GetRequiredService<AmazonS3AssetStore>();

    public async Task InitializeAsync()
    {
        // From: https://console.aws.amazon.com/iam/home?region=eu-central-1#/users/s3?section=security_credentials
        Services =
            new ServiceCollection()
                .AddAmazonS3AssetStore(TestHelpers.Configuration)
                .BuildServiceProvider();

        foreach (var service in Services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }

    public async Task DisposeAsync()
    {
        foreach (var service in Services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }
    }
}
