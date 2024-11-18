// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using tusdotnet.Interfaces;

namespace Squidex.Assets.Internal;

public sealed class AssetFileLockProvider(IAssetStore assetStore) : ITusFileLockProvider
{
    public Task<ITusFileLock> AquireLock(string fileId)
    {
        return Task.FromResult<ITusFileLock>(new AssetFileLock(assetStore, fileId));
    }
}
