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

namespace Squidex.Assets.TusAdapter;

public sealed class AssetTusFile(
    string id,
    TusMetadata tusMetadata,
    Dictionary<string, string> metadata,
    Dictionary<string, Metadata> metadataRaw,
    Stream stream,
    Action<AssetTusFile> disposed)
    : IAssetFile, ITusFile, IAsyncDisposable, IDisposable
{
    public string Id { get; } = id;

    public string FileName { get; } = GetFileName(metadata);

    public string MimeType { get; } = GetMimeType(metadata);

    public long FileSize { get; } = stream.Length;

    public Dictionary<string, string> Metadata { get; } = metadata;

    public Dictionary<string, Metadata> MetadataRaw { get; } = metadataRaw;

    internal TusMetadata TusMetadata { get; } = tusMetadata;

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
