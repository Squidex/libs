// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting;
using Xunit;

namespace Squidex.Assets;

public sealed class AmazonS3AssetStoreFixture : IAsyncLifetime
{
    private IServiceProvider services;

    public AmazonS3AssetStore Store => services.GetRequiredService<AmazonS3AssetStore>();

    public async Task DisposeAsync()
    {
        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }
    }

    public async Task InitializeAsync()
    {
        // From: https://console.aws.amazon.com/iam/home?region=eu-central-1#/users/s3?section=security_credentials
        services =
            new ServiceCollection()
                .AddAmazonS3AssetStore(TestHelpers.Configuration)
                .BuildServiceProvider();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
