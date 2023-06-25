// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FakeItEasy;

namespace Squidex.Assets;

public sealed class FTPAssetStoreFixture : IDisposable
{
    public FTPAssetStore AssetStore { get; }

    public FTPAssetStoreFixture()
    {
        var services =
            new ServiceCollection()
                .AddSingleton(A.Fake<ILogger<FTPAssetStore>>())
                .AddFTPAssetStore(TestHelpers.Configuration)
                .BuildServiceProvider();

        AssetStore = services.GetRequiredService<FTPAssetStore>();
        AssetStore.InitializeAsync(default).Wait();
    }

    public void Dispose()
    {
    }
}
