// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FakeItEasy;

namespace Squidex.Assets;

public sealed class FolderAssetStoreFixture : IDisposable
{
    public string TestFolder { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public FolderAssetStore AssetStore { get; }

    public FolderAssetStoreFixture()
    {
        var services =
            new ServiceCollection()
                .AddFolderAssetStore(TestHelpers.Configuration, config =>
                {
                    config.Path = TestFolder;
                })
                .AddSingleton(A.Dummy<ILogger<FolderAssetStore>>())
                .BuildServiceProvider();

        AssetStore = services.GetRequiredService<FolderAssetStore>();
        AssetStore.InitializeAsync(default).Wait();
    }

    public void Dispose()
    {
        if (Directory.Exists(TestFolder))
        {
            Directory.Delete(TestFolder, true);
        }
    }
}
