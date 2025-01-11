// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FakeItEasy;
using Squidex.Hosting;
using Xunit;

namespace Squidex.Assets;

public sealed class FTPAssetStoreFixture : IAsyncLifetime
{
    private IServiceProvider services;

    public FTPAssetStore Store => services.GetRequiredService<FTPAssetStore>();

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
                .AddSingleton(A.Fake<ILogger<FTPAssetStore>>())
                .AddFTPAssetStore(TestHelpers.Configuration)
                .BuildServiceProvider();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
