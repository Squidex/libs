// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    internal static class AssetStorageExtensions
    {
        public static async Task UploadAndResetAsync(this IAssetStore assetStore, string name, Stream stream)
        {
            try
            {
                await assetStore.UploadAsync(name, stream);
            }
            finally
            {
                stream.Position = 0;
            }
        }
    }
}
