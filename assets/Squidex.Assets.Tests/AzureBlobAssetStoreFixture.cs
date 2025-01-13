// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.Azure;
using Squidex.Hosting;
using Xunit;

namespace Squidex.Assets;

public sealed class AzureBlobAssetStoreFixture : IAsyncLifetime
{
    private IServiceProvider services;

    public AzureBlobAssetStore Store => services.GetRequiredService<AzureBlobAssetStore>();

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
                .AddAzureBlobAssetStore(TestHelpers.Configuration)
                .BuildServiceProvider();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
