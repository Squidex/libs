// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Squidex.Assets.Internal;

namespace Squidex.Assets;

public sealed class AmazonS3AssetStore : DisposableObjectBase, IAssetStore
{
    private const int BufferSize = 81920;
    private readonly AmazonS3AssetOptions options;
    private TransferUtility transferUtility;
    private IAmazonS3 s3Client;

    public AmazonS3AssetStore(AmazonS3AssetOptions options)
    {
        Guard.NotNull(options, nameof(options));
        Guard.NotNullOrEmpty(options.Bucket, nameof(options.Bucket));
        Guard.NotNullOrEmpty(options.AccessKey, nameof(options.AccessKey));
        Guard.NotNullOrEmpty(options.SecretKey, nameof(options.SecretKey));

        this.options = options;
    }

    protected override void DisposeObject(bool disposing)
    {
        if (disposing)
        {
            s3Client?.Dispose();

            transferUtility?.Dispose();
        }
    }

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        try
        {
            var amazonS3Config = new AmazonS3Config { ForcePathStyle = options.ForcePathStyle };

            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            {
                amazonS3Config.ServiceURL = options.ServiceUrl;
            }
            else
            {
                amazonS3Config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.RegionName);
            }

            s3Client = new AmazonS3Client(options.AccessKey, options.SecretKey, amazonS3Config);

            transferUtility = new TransferUtility(s3Client);

            var exists = await s3Client.DoesS3BucketExistAsync(options.Bucket);

            if (!exists)
            {
                throw new AssetStoreException($"Cannot connect to Amazon S3 bucket '{options.Bucket}'.");
            }
        }
        catch (AmazonS3Exception ex)
        {
            throw new AssetStoreException($"Cannot connect to Amazon S3 bucket '{options.Bucket}'.", ex);
        }
    }

    public async Task<long> GetSizeAsync(string fileName,
        CancellationToken ct = default)
    {
        var key = GetKey(fileName, nameof(fileName));

        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = options.Bucket,
                Key = key
            };

            var metadata = await s3Client.GetObjectMetadataAsync(request, ct);

            return metadata.ContentLength;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AssetNotFoundException(fileName, ex);
        }
    }

    public async Task CopyAsync(string sourceFileName, string targetFileName,
        CancellationToken ct = default)
    {
        var keySource = GetKey(sourceFileName, nameof(sourceFileName));
        var keyTarget = GetKey(targetFileName, nameof(targetFileName));

        try
        {
            await EnsureNotExistsAsync(keyTarget, targetFileName, ct);

            var request = new CopyObjectRequest
            {
                SourceBucket = options.Bucket,
                SourceKey = keySource,
                DestinationBucket = options.Bucket,
                DestinationKey = keyTarget
            };

            await s3Client.CopyObjectAsync(request, ct);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AssetNotFoundException(sourceFileName, ex);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new AssetAlreadyExistsException(targetFileName);
        }
    }

    public async Task DownloadAsync(string fileName, Stream stream, BytesRange range = default,
        CancellationToken ct = default)
    {
        Guard.NotNull(stream, nameof(stream));

        var key = GetKey(fileName, nameof(fileName));

        try
        {
            var request = new GetObjectRequest
            {
                BucketName = options.Bucket,
                Key = key
            };

            if (range.IsDefined)
            {
                request.ByteRange = new ByteRange(range.ToString());
            }

            using (var response = await s3Client.GetObjectAsync(request, ct))
            {
                await response.ResponseStream.CopyToAsync(stream, BufferSize, ct);
            }
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AssetNotFoundException(fileName, ex);
        }
    }

    public async Task<long> UploadAsync(string fileName, Stream stream, bool overwrite = false,
        CancellationToken ct = default)
    {
        Guard.NotNull(stream, nameof(stream));

        var key = GetKey(fileName, nameof(fileName));

        try
        {
            if (!overwrite)
            {
                await EnsureNotExistsAsync(key, fileName, ct);
            }

            var request = new TransferUtilityUploadRequest
            {
                BucketName = options.Bucket,
                Key = key
            };

            if (stream.GetLengthOrZero() <= 0)
            {
                var tempFileName = Path.GetTempFileName();

                var tempStream = new FileStream(tempFileName,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.Delete,
                    1024 * 16,
                    FileOptions.Asynchronous |
                    FileOptions.DeleteOnClose |
                    FileOptions.SequentialScan);

                await using (tempStream)
                {
                    await stream.CopyToAsync(tempStream, ct);

                    request.InputStream = tempStream;

                    await transferUtility.UploadAsync(request, ct);
                }
            }
            else
            {
                request.InputStream = new SeekFakerStream(stream);
                request.AutoCloseStream = false;

                await transferUtility.UploadAsync(request, ct);
            }

            return -1;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new AssetAlreadyExistsException(fileName);
        }
    }

    public async Task DeleteByPrefixAsync(string prefix,
        CancellationToken ct = default)
    {
        var key = GetKey(prefix, nameof(prefix));

        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = options.Bucket
            };

            string? continuationToken = null;

            while (!ct.IsCancellationRequested)
            {
                var items = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = options.Bucket,
                    Prefix = key,
                    ContinuationToken = continuationToken
                }, ct);

                foreach (var item in items.S3Objects)
                {
                    try
                    {
                        request.Key = item.Key;

                        await s3Client.DeleteObjectAsync(request, ct);
                    }
                    catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        continue;
                    }
                }

                continuationToken = items.NextContinuationToken;

                if (string.IsNullOrWhiteSpace(continuationToken))
                {
                    break;
                }
            }
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }
    }

    public async Task DeleteAsync(string fileName,
        CancellationToken ct = default)
    {
        var key = GetKey(fileName, nameof(fileName));

        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = options.Bucket,
                Key = key
            };

            await s3Client.DeleteObjectAsync(request, ct);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }
    }

    private string GetKey(string fileName, string parameterName)
    {
        Guard.NotNullOrEmpty(fileName, parameterName);

        fileName = fileName.Replace("\\", "/", System.StringComparison.Ordinal);

        if (!string.IsNullOrWhiteSpace(options.BucketFolder))
        {
            return $"{options.BucketFolder}/{fileName}";
        }
        else
        {
            return fileName;
        }
    }

    private async Task EnsureNotExistsAsync(string key, string fileName,
        CancellationToken ct)
    {
        try
        {
            await s3Client.GetObjectAsync(options.Bucket, key, ct);
        }
        catch
        {
            return;
        }

        throw new AssetAlreadyExistsException(fileName);
    }
}
