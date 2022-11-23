// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using tusdotnet.Interfaces;

namespace Squidex.Assets.Internal;

internal sealed class AssetFileLock : ITusFileLock
{
    private readonly IAssetStore assetStore;
    private readonly string filePath;

    public AssetFileLock(IAssetStore assetStore, string fileId)
    {
        this.assetStore = assetStore;

        filePath = $"locks/{fileId}.lock";
    }

    public async Task<bool> Lock()
    {
        try
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(filePath));

            await assetStore.UploadAsync(filePath, stream, false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task ReleaseIfHeld()
    {
        try
        {
            await assetStore.DeleteAsync(filePath);
        }
        catch
        {
            return;
        }
    }
}
