// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.GoogleCloud;
using Squidex.Hosting;
using Xunit;

namespace Squidex.Assets;

public sealed class GoogleCloudAssetStoreFixture : IAsyncLifetime
{
    private IServiceProvider services;

    public GoogleCloudAssetStore Store => services.GetRequiredService<GoogleCloudAssetStore>();

    public async Task DisposeAsync()
    {
        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }
    }

    public async Task InitializeAsync()
    {
        services =
            new ServiceCollection()
                .AddGoogleCloudAssetStore(TestHelpers.Configuration)
                .BuildServiceProvider();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
