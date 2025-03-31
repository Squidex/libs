// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Squidex.Hosting;

namespace Squidex.Assets.Mongo;

public sealed class MongoGridFsAssetStore(IGridFSBucket<string> bucket) : IAssetStore, IInitializable
{
    private static readonly FilterDefinitionBuilder<GridFSFileInfo<string>> Filters = Builders<GridFSFileInfo<string>>.Filter;
    private static readonly GridFSDownloadOptions DownloadDefault = new GridFSDownloadOptions();
    private static readonly GridFSDownloadOptions DownloadSeekable = new GridFSDownloadOptions { Seekable = true };

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        try
        {
            await bucket.Database.ListCollectionsAsync(cancellationToken: ct);
        }
        catch (MongoException ex)
        {
            throw new AssetStoreException($"Cannot connect to Mongo GridFS bucket '{bucket.Options.BucketName}'.", ex);
        }
    }

    public async Task<long> GetSizeAsync(string fileName,
        CancellationToken ct = default)
    {
        var name = GetFileName(fileName, nameof(fileName));

        var fileQuery = await bucket.FindAsync(Filters.Eq(x => x.Id, name), cancellationToken: ct);
        var fileObject = await fileQuery.FirstOrDefaultAsync(ct);

        return fileObject == null ? throw new AssetNotFoundException(fileName) : fileObject.Length;
    }

    public async Task CopyAsync(string sourceFileName, string targetFileName,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFileName);

        var sourceName = GetFileName(sourceFileName, nameof(sourceFileName));

        try
        {
            await using (var readStream = await bucket.OpenDownloadStreamAsync(sourceName, cancellationToken: ct))
            {
                await UploadAsync(targetFileName, readStream, false, ct);
            }
        }
        catch (GridFSFileNotFoundException ex)
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
            var options = range.IsDefined ? DownloadSeekable : DownloadDefault;

            await using (var readStream = await bucket.OpenDownloadStreamAsync(name, options, ct))
            {
                await readStream.CopyToAsync(stream, range, ct);
            }
        }
        catch (GridFSFileNotFoundException ex)
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
            if (overwrite)
            {
                await DeleteAsync(fileName, ct);
            }

            await bucket.UploadFromStreamAsync(name, name, stream, cancellationToken: ct);

            return -1;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new AssetAlreadyExistsException(fileName);
        }
        catch (MongoBulkWriteException<BsonDocument> ex) when (ex.WriteErrors.Any(x => x.Category == ServerErrorCategory.DuplicateKey))
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
            var match = new BsonRegularExpression($"^{name}");

            var fileQuery = await bucket.FindAsync(Filters.Regex(x => x.Id, match), cancellationToken: ct);

            await fileQuery.ForEachAsync(async file =>
            {
                try
                {
                    await bucket.DeleteAsync(file.Id, ct);
                }
                catch (GridFSFileNotFoundException)
                {
                    return;
                }
            }, ct);
        }
        catch (GridFSFileNotFoundException)
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
            await bucket.DeleteAsync(name, ct);
        }
        catch (GridFSFileNotFoundException)
        {
            return;
        }
    }

    private static string GetFileName(string fileName, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, parameterName);

        return FilePathHelper.EnsureThatPathIsChildOf(fileName.Replace('\\', '/'), "./");
    }
}
