// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FakeItEasy;
using FluentFTP;

namespace Squidex.Assets;

public sealed class FTPAssetStoreFixture : IDisposable
{
    public FTPAssetStore AssetStore { get; }

    public FTPAssetStoreFixture()
    {
        AssetStore = new FTPAssetStore(
            () => new AsyncFtpClient(
                TestHelpers.Configuration["ftp:serverHost"],
                TestHelpers.Configuration["ftp:username"],
                TestHelpers.Configuration["ftp:userPassword"]),
            new FTPAssetOptions
            {
                Path = TestHelpers.Configuration["ftp:path"]
            },
            A.Fake<ILogger<FTPAssetStore>>());
        AssetStore.InitializeAsync(default).Wait();
    }

    public void Dispose()
    {
    }
}
