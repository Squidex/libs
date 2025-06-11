// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.FTP;
using Squidex.Hosting;

namespace Squidex.Assets;

public sealed class FTPAssetStoreFixture : IAsyncLifetime
{
    public IServiceProvider Services { get; private set; }

    public FTPAssetStore Store => Services.GetRequiredService<FTPAssetStore>();

    public async Task InitializeAsync()
    {
        Services =
            new ServiceCollection()
                .AddSingleton(A.Fake<ILogger<FTPAssetStore>>())
                .AddFTPAssetStore(TestHelpers.Configuration)
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
