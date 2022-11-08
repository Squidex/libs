// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets;

namespace TutTestServer;

public sealed class Initializer : IHostedService
{
    private readonly IEnumerable<IAssetStore> assetStores;
    private readonly IAssetKeyValueStore<TusMetadata> assetKeyValueStore;

    public Initializer(IEnumerable<IAssetStore> assetStores, IAssetKeyValueStore<TusMetadata> assetKeyValueStore)
    {
        this.assetStores = assetStores;
        this.assetKeyValueStore = assetKeyValueStore;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        foreach (var assetStore in assetStores)
        {
            await assetStore.InitializeAsync(cancellationToken);
        }

        await assetKeyValueStore.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
