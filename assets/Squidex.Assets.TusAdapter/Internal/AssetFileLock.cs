// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using tusdotnet.Interfaces;

namespace Squidex.Assets.TusAdapter.Internal;

internal sealed class AssetFileLock(IAssetStore assetStore, string fileId) : ITusFileLock
{
    private readonly string filePath = $"locks/{fileId}.lock";

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
