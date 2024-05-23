// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Parsers;

namespace Squidex.Assets;

public sealed class AssetTusFile : IAssetFile, ITusFile, IAsyncDisposable, IDisposable
{
    private readonly Stream stream;
    private readonly Action<AssetTusFile> disposed;

    public string Id { get; }

    public string FileName { get; }

    public string MimeType { get; }

    public long FileSize { get; }

    public Dictionary<string, string> Metadata { get; }

    public Dictionary<string, Metadata> MetadataRaw { get; }

    internal TusMetadata TusMetadata { get; }

    public static AssetTusFile Create(string id, TusMetadata tusMetadata, Stream stream, Action<AssetTusFile> disposed)
    {
        var metadataRaw = MetadataParser.ParseAndValidate(MetadataParsingStrategy.AllowEmptyValues, tusMetadata.UploadMetadata).Metadata;

        var metadata = new Dictionary<string, string>();

        foreach (var (key, value) in metadataRaw)
        {
            metadata[key] = value.GetString(Encoding.UTF8).Trim();
        }

        return new AssetTusFile(id, tusMetadata, metadata, metadataRaw, stream, disposed);
    }

    public AssetTusFile(
        string id,
        TusMetadata tusMetadata,
        Dictionary<string, string> metadata,
        Dictionary<string, Metadata> metadataRaw,
        Stream stream,
        Action<AssetTusFile> disposed)
    {
        Id = id;

        this.stream = stream;

        FileSize = stream.Length;
        FileName = GetFileName(metadata);
        MimeType = GetMimeType(metadata);

        Metadata = metadata;
        MetadataRaw = metadataRaw;
        TusMetadata = tusMetadata;

        this.disposed = disposed;
    }

    private static string GetFileName(Dictionary<string, string> metadata)
    {
        var result = metadata.FirstOrDefault(x => string.Equals(x.Key, "fileName", StringComparison.OrdinalIgnoreCase)).Value;

        if (!string.IsNullOrWhiteSpace(result))
        {
            return result;
        }

        return "Unknown.blob";
    }

    private static string GetMimeType(Dictionary<string, string> metadata)
    {
        var result = metadata.FirstOrDefault(x => string.Equals(x.Key, "fileType", StringComparison.OrdinalIgnoreCase)).Value;

        if (!string.IsNullOrWhiteSpace(result))
        {
            return result;
        }

        result = metadata.FirstOrDefault(x => string.Equals(x.Key, "mimeType", StringComparison.OrdinalIgnoreCase)).Value;

        if (!string.IsNullOrWhiteSpace(result))
        {
            return result;
        }

        return "application/octet-stream";
    }

    public void Dispose()
    {
        disposed(this);

        stream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        disposed(this);

        return stream.DisposeAsync();
    }

    public Stream OpenRead()
    {
        return new NonDisposingStream(stream);
    }

    public Task<Stream> GetContentAsync(
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Stream>(new NonDisposingStream(stream));
    }

    public Task<Dictionary<string, Metadata>> GetMetadataAsync(
        CancellationToken cancellationToken)
    {
        return Task.FromResult(MetadataRaw);
    }
}
