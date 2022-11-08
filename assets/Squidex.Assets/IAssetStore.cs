// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public interface IAssetStore
{
    string? GeneratePublicUrl(string fileName)
    {
        return null;
    }

    Task InitializeAsync(
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    Task<long> GetSizeAsync(string fileName,
        CancellationToken ct = default);

    Task<long> UploadAsync(string fileName, Stream stream, bool overwrite = false,
        CancellationToken ct = default);

    Task CopyAsync(string sourceFileName, string targetFileName,
        CancellationToken ct = default);

    Task DownloadAsync(string fileName, Stream stream, BytesRange range = default,
        CancellationToken ct = default);

    Task DeleteByPrefixAsync(string prefix,
        CancellationToken ct = default);

    Task DeleteAsync(string fileName,
        CancellationToken ct = default);
}
