// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Squidex.Assets.Internal;
using tusdotnet.Interfaces;
using tusdotnet.Models;

#pragma warning disable MA0106 // Avoid closure by using an overload with the 'factoryArgument' parameter

namespace Squidex.Assets;

public sealed class AssetTusStore(IAssetStore assetStore, IAssetKeyValueStore<TusMetadata> keyValueStore) :
    ITusExpirationStore,
    ITusCreationDeferLengthStore,
    ITusCreationStore,
    ITusReadableStore,
    ITusTerminationStore,
    ITusStore
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromDays(2);
    private readonly ConcurrentDictionary<string, Task<AssetTusFile>> files = new ConcurrentDictionary<string, Task<AssetTusFile>>();

    public async Task<string> CreateFileAsync(long uploadLength, string metadata,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString();

        var metadataObj = new TusMetadata
        {
            Created = true,
            UploadLength = uploadLength,
            UploadMetadata = metadata,
            Expires = DateTimeOffset.UtcNow.Add(DefaultExpiration)
        };

        await SetMetadataAsync(id, metadataObj, cancellationToken);

        return id;
    }

    public async Task SetExpirationAsync(string fileId, DateTimeOffset expires,
        CancellationToken cancellationToken)
    {
        var metadata = (await GetMetadataAsync(fileId, cancellationToken)) ?? new TusMetadata();

        metadata.Expires = expires;

        await SetMetadataAsync(fileId, metadata, cancellationToken);
    }

    public async Task SetUploadLengthAsync(string fileId, long uploadLength,
        CancellationToken cancellationToken)
    {
        var metadata = (await GetMetadataAsync(fileId, cancellationToken)) ?? new TusMetadata();

        metadata.UploadLength = uploadLength;

        await SetMetadataAsync(fileId, metadata, cancellationToken);
    }

    public async Task<ITusFile?> GetFileAsync(string fileId,
        CancellationToken cancellationToken)
    {
        var metadata = await GetMetadataAsync(fileId, cancellationToken);

        if (metadata == null || metadata.WrittenParts == 0)
        {
            return null;
        }

        async Task<AssetTusFile> CreateFileAsync(string fileId, TusMetadata metadata,
            CancellationToken ct)
        {
            var tempStream = TempHelper.GetTempStream();

            for (var i = 0; i < metadata.WrittenParts; i++)
            {
                try
                {
                    await assetStore.DownloadAsync(PartName(fileId, i), tempStream, default, ct);
                }
                catch (AssetNotFoundException)
                {
                    continue;
                }
            }

            await CleanupAsync(metadata, default);

            return AssetTusFile.Create(fileId, metadata, tempStream, file =>
            {
                files.TryRemove(file.Id, out _);
            });
        }

        return await files.GetOrAdd(fileId, id => CreateFileAsync(id, metadata, cancellationToken));
    }

    public async Task<long> AppendDataAsync(string fileId, Stream stream,
        CancellationToken cancellationToken)
    {
        var metadata = await GetMetadataAsync(fileId, cancellationToken);

        if (metadata == null)
        {
            return 0;
        }

        if (stream.GetLengthOrZero() > 0 && metadata.UploadLength.HasValue)
        {
            var sizeAfterUpload = metadata.UploadLength + stream.GetLengthOrZero();

            if (metadata.UploadLength + stream.Length > metadata.UploadLength.Value)
            {
                throw new TusStoreException($"Stream contains more data than the file's upload length. Stream data: {sizeAfterUpload}, upload length: {metadata.UploadLength}.");
            }
        }

        await SetMetadataAsync(fileId, metadata, cancellationToken);

        var writtenBytes = 0L;

        using (var cancellableStream = new CancellableStream(stream, cancellationToken))
        {
            var partName = PartName(fileId, metadata.WrittenParts);

            // Do not flow cancellation token because it is handled by the stream that stops silently.
            writtenBytes = await assetStore.UploadAsync(partName, cancellableStream, true, default);

            if (writtenBytes < 0)
            {
                writtenBytes = await assetStore.GetSizeAsync(partName, cancellationToken);
            }

            // Also update the size in the metadata.
            metadata.WrittenBytes += writtenBytes;
            metadata.WrittenParts++;

            // Do not cancel here.
            await SetMetadataAsync(fileId, metadata, default);
        }

        if (metadata.UploadLength.HasValue && metadata.WrittenBytes > metadata.UploadLength.Value)
        {
            throw new TusStoreException($"Stream contains more data than the file's upload length. Stream data: {metadata.WrittenBytes}, upload length: {metadata.UploadLength}.");
        }

        return writtenBytes;
    }

    public async Task<bool> FileExistAsync(string fileId,
        CancellationToken cancellationToken)
    {
        var metadata = await GetMetadataAsync(fileId, cancellationToken);

        return metadata != null && (metadata.WrittenBytes > 0 || metadata.Created);
    }

    public async Task<IEnumerable<string>> GetExpiredFilesAsync(
        CancellationToken cancellationToken)
    {
        var result = new List<string>();

        var expirations = keyValueStore.GetExpiredEntriesAsync(DateTimeOffset.UtcNow, cancellationToken);

        await foreach (var (_, value) in expirations.WithCancellation(cancellationToken))
        {
            result.Add(value.Id);
        }

        return result;
    }

    public async Task<long> GetUploadOffsetAsync(string fileId,
        CancellationToken cancellationToken)
    {
        var metadata = await GetMetadataAsync(fileId, cancellationToken);

        return metadata?.WrittenBytes ?? 0;
    }

    public async Task<long?> GetUploadLengthAsync(string fileId,
        CancellationToken cancellationToken)
    {
        var metadata = await GetMetadataAsync(fileId, cancellationToken);

        return metadata?.UploadLength;
    }

    public async Task<string?> GetUploadMetadataAsync(string fileId,
        CancellationToken cancellationToken)
    {
        var metadata = await GetMetadataAsync(fileId, cancellationToken);

        return metadata?.UploadMetadata;
    }

    public async Task<DateTimeOffset?> GetExpirationAsync(string fileId,
        CancellationToken cancellationToken)
    {
        var metadata = await GetMetadataAsync(fileId, cancellationToken);

        return metadata?.Expires;
    }

    private async Task<TusMetadata?> GetMetadataAsync(string fileId,
        CancellationToken ct)
    {
        var key = Key(fileId);

        return await keyValueStore.GetAsync(key, ct);
    }

    public async Task<int> RemoveExpiredFilesAsync(
        CancellationToken cancellationToken)
    {
        var deletionCount = 0;

        var expirations = keyValueStore.GetExpiredEntriesAsync(DateTimeOffset.UtcNow, cancellationToken);

        await foreach (var (_, expiration) in expirations.WithCancellation(cancellationToken))
        {
            await CleanupAsync(expiration, cancellationToken);
        }

        return deletionCount;
    }

    public async Task DeleteFileAsync(string fileId,
        CancellationToken cancellationToken)
    {
        var metadata = await GetMetadataAsync(fileId, cancellationToken);

        if (metadata == null)
        {
            return;
        }

        await CleanupAsync(metadata, cancellationToken);
    }

    private async Task CleanupAsync(TusMetadata metadata,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < metadata.WrittenParts; i++)
        {
            await assetStore.DeleteAsync(PartName(metadata.Id, i), cancellationToken);
        }

        await keyValueStore.DeleteAsync(Key(metadata.Id), cancellationToken);
    }

    private Task SetMetadataAsync(string fileId, TusMetadata metadata,
        CancellationToken ct)
    {
        var key = Key(fileId);

        metadata.Id = fileId;

        if (metadata.Expires == default)
        {
            metadata.Expires = DateTimeOffset.UtcNow.Add(DefaultExpiration);
        }

        return keyValueStore.SetAsync(key, metadata, metadata.Expires!.Value, ct);
    }

    private static string PartName(string fileId, int index)
    {
        return $"tus/{fileId}_{index}";
    }

    private static string Key(string fileId)
    {
        return $"TUSFILE_{fileId}";
    }
}
