// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public sealed class NoopAssetStore : IAssetStore
    {
        public Task<long> GetSizeAsync(string fileName,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public Task CopyAsync(string sourceFileName, string fileName,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public Task DownloadAsync(string fileName, Stream stream, BytesRange range = default,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public Task<long> UploadAsync(string fileName, Stream stream, bool overwrite = false,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteByPrefixAsync(string prefix,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(string fileName,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }
}
