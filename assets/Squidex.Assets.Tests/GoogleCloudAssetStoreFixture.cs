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
    public IServiceProvider Services { get; private set; }

    public GoogleCloudAssetStore Store => Services.GetRequiredService<GoogleCloudAssetStore>();

    public async Task InitializeAsync()
    {
        Services =
            new ServiceCollection()
                .AddGoogleCloudAssetStore(TestHelpers.Configuration)
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
