// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public static class AssetStoreExtensions
{
    public static async Task UploadTestAssetAsync(this IAssetStore assetStore,
        CancellationToken ct)
    {
        var testGuid = Guid.NewGuid();
        var testFile = $"tests/{testGuid}";

        await assetStore.UploadAsync(testFile, new MemoryStream(testGuid.ToByteArray()), true, ct);
        await assetStore.DeleteAsync(testFile, ct);
    }
}
