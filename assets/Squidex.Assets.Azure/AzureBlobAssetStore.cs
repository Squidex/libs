// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Squidex.Hosting;

namespace Squidex.Assets;

public class AzureBlobAssetStore : IAssetStore, IInitializable
{
    private static readonly BlobUploadOptions NoOverwriteUpload = new BlobUploadOptions
    {
        Conditions = new BlobRequestConditions
        {
            IfNoneMatch = new ETag("*")
        }
    };
    private static readonly BlobCopyFromUriOptions NoOverwriteCopy = new BlobCopyFromUriOptions
    {
        DestinationConditions = new BlobRequestConditions
        {
            IfNoneMatch = new ETag("*")
        }
    };
    private readonly AzureBlobAssetOptions options;
    private BlobContainerClient blobContainer;
    private BlobContainerProperties blobContainerProperties;

    public AzureBlobAssetStore(IOptions<AzureBlobAssetOptions> options)
    {
        this.options = options.Value;
    }

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(options.ConnectionString);

            blobContainer = blobServiceClient.GetBlobContainerClient(options.ContainerName);

            await blobContainer.CreateIfNotExistsAsync(cancellationToken: ct);

            blobContainerProperties = await blobContainer.GetPropertiesAsync(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            throw new AssetStoreException($"Cannot connect to blob container '{options.ContainerName}'.", ex);
        }
    }

    public string? GeneratePublicUrl(string fileName)
    {
        var name = GetFileName(fileName, nameof(fileName));

        if (blobContainerProperties.PublicAccess != PublicAccessType.Blob)
        {
            var blob = blobContainer.GetBlobClient(name);

            return blob.Uri.ToString();
        }

        return null;
    }

    public async Task<long> GetSizeAsync(string fileName,
        CancellationToken ct = default)
    {
        var name = GetFileName(fileName, nameof(fileName));

        try
        {
            var blob = blobContainer.GetBlobClient(name);

            var properties = await blob.GetPropertiesAsync(cancellationToken: ct);

            return properties.Value.ContentLength;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
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
            var blobSource = blobContainer.GetBlobClient(sourceName);
            var blobTarget = blobContainer.GetBlobClient(targetName);

            await blobTarget.StartCopyFromUriAsync(blobSource.Uri, NoOverwriteCopy, ct);

            BlobProperties targetProperties;
            do
            {
                targetProperties = await blobTarget.GetPropertiesAsync(cancellationToken: ct);

                await Task.Delay(50, ct);
            }
            while (targetProperties.CopyStatus == CopyStatus.Pending);

            if (targetProperties.CopyStatus != CopyStatus.Success)
            {
                throw new AssetStoreException($"Copy of temporary file failed: {targetProperties.CopyStatus}");
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            throw new AssetAlreadyExistsException(targetFileName, ex);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new AssetNotFoundException(sourceFileName, ex);
        }
    }

    public async Task DownloadAsync(string fileName, Stream stream, BytesRange range = default,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var name = GetFileName(fileName, nameof(fileName));

        try
        {
            var blob = blobContainer.GetBlobClient(name);

            var downloadOptions = new BlobDownloadOptions
            {
                Range = new HttpRange(range.From ?? 0, range.To)
            };

            var result = await blob.DownloadStreamingAsync(downloadOptions, ct);

            await using (result.Value.Content)
            {
                await result.Value.Content.CopyToAsync(stream, ct);
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new AssetNotFoundException(fileName, ex);
        }
    }

    public async Task<long> UploadAsync(string fileName, Stream stream, bool overwrite = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var name = GetFileName(fileName, nameof(fileName));

        try
        {
            var blob = blobContainer.GetBlobClient(name);

            await blob.UploadAsync(stream, overwrite ? null : NoOverwriteUpload, ct);

            return -1;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            throw new AssetAlreadyExistsException(fileName, ex);
        }
    }

    public async Task DeleteByPrefixAsync(string prefix,
        CancellationToken ct = default)
    {
        var name = GetFileName(prefix, nameof(prefix));

        var items = blobContainer.GetBlobsAsync(prefix: name, cancellationToken: ct);

        await foreach (var item in items.WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();

            await blobContainer.DeleteBlobIfExistsAsync(item.Name, cancellationToken: ct);
        }
    }

    public Task DeleteAsync(string fileName,
        CancellationToken ct = default)
    {
        var name = GetFileName(fileName, nameof(fileName));

        return blobContainer.DeleteBlobIfExistsAsync(name, cancellationToken: ct);
    }

    private static string GetFileName(string fileName, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, parameterName);

        return FilePathHelper.EnsureThatPathIsChildOf(fileName.Replace('\\', '/'), "./");
    }
}
