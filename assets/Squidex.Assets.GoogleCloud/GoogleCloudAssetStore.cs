// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net;
using System.Net.Http.Headers;
using Google;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using Squidex.Assets.Internal;
using Squidex.Hosting;

namespace Squidex.Assets;

public sealed class GoogleCloudAssetStore : IAssetStore, IInitializable
{
    private static readonly UploadObjectOptions IfNotExists = new UploadObjectOptions { IfGenerationMatch = 0 };
    private static readonly CopyObjectOptions IfNotExistsCopy = new CopyObjectOptions { IfGenerationMatch = 0 };
    private readonly string bucketName;
    private StorageClient storageClient;

    public GoogleCloudAssetStore(IOptions<GoogleCloudAssetOptions> options)
    {
        bucketName = options.Value.Bucket;
    }

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        try
        {
            storageClient = await StorageClient.CreateAsync();

            await storageClient.GetBucketAsync(bucketName, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            throw new AssetStoreException($"Cannot connect to google cloud bucket '{bucketName}'.", ex);
        }
    }

    public async Task<long> GetSizeAsync(string fileName,
        CancellationToken ct = default)
    {
        var name = GetFileName(fileName, nameof(fileName));

        try
        {
            var obj = await storageClient.GetObjectAsync(bucketName, name, null, ct);

            if (!obj.Size.HasValue)
            {
                throw new AssetNotFoundException(fileName);
            }

            return (long)obj.Size.Value;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new AssetNotFoundException(fileName, ex);
        }
    }

    public async Task CopyAsync(string sourceFileName, string targetFileName,
        CancellationToken ct = default)
    {
        var sourceName = GetFileName(sourceFileName, nameof(sourceFileName));
        var targetName = GetFileName(targetFileName, nameof(targetFileName));

        try
        {
            await storageClient.CopyObjectAsync(bucketName, sourceName, bucketName, targetName, IfNotExistsCopy, ct);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new AssetNotFoundException(sourceName, ex);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new AssetAlreadyExistsException(targetFileName);
        }
    }

    public async Task DownloadAsync(string fileName, Stream stream, BytesRange range = default,
        CancellationToken ct = default)
    {
        var name = GetFileName(fileName, nameof(fileName));

        try
        {
            var downloadOptions = new DownloadObjectOptions();

            if (range.IsDefined)
            {
                downloadOptions.Range = new RangeHeaderValue(range.From, range.To);
            }

            await storageClient.DownloadObjectAsync(bucketName, name, stream, downloadOptions, ct);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new AssetNotFoundException(fileName, ex);
        }
    }

    public async Task<long> UploadAsync(string fileName, Stream stream, bool overwrite = false,
        CancellationToken ct = default)
    {
        var name = GetFileName(fileName, nameof(fileName));

        try
        {
            var result = await storageClient.UploadObjectAsync(bucketName, name, "application/octet-stream", stream, overwrite ? null : IfNotExists, ct);

            if (result.Size.HasValue)
            {
                return (long)result.Size.Value;
            }

            return -1;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new AssetAlreadyExistsException(fileName);
        }
    }

    public async Task DeleteByPrefixAsync(string prefix,
        CancellationToken ct = default)
    {
        var name = GetFileName(prefix, nameof(prefix));

        try
        {
            var items = storageClient.ListObjectsAsync(bucketName, name);

            await foreach (var item in items.WithCancellation(ct))
            {
                try
                {
                    await storageClient.DeleteObjectAsync(item.Bucket, item.Name, cancellationToken: ct);
                }
                catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    continue;
                }
            }
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return;
        }
    }

    public async Task DeleteAsync(string fileName,
        CancellationToken ct = default)
    {
        var name = GetFileName(fileName, nameof(fileName));

        try
        {
            await storageClient.DeleteObjectAsync(bucketName, name, cancellationToken: ct);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return;
        }
    }

    private static string GetFileName(string fileName, string parameterName)
    {
        Guard.NotNullOrEmpty(fileName, parameterName);

        return FilePathHelper.EnsureThatPathIsChildOf(fileName.Replace('\\', '/'), "./");
    }
}
