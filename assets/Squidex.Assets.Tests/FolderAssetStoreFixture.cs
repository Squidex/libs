// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting;
using Xunit;

namespace Squidex.Assets;

public sealed class FolderAssetStoreFixture : IAsyncLifetime
{
    public IServiceProvider Services { get; private set; }

    public string TestFolder { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public FolderAssetStore Store => Services.GetRequiredService<FolderAssetStore>();

    public async Task InitializeAsync()
    {
        Services =
            new ServiceCollection()
                .AddFolderAssetStore(TestHelpers.Configuration, config =>
                {
                    config.Path = TestFolder;
                })
                .AddLogging()
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

        if (Directory.Exists(TestFolder))
        {
            Directory.Delete(TestFolder, true);
        }
    }
}
