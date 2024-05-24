// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public sealed class TempAssetFile : IAssetFile
{
    private readonly Stream stream = TempHelper.GetTempStream();

    public long FileSize => stream.Length;

    public string FileName { get; }

    public string MimeType { get; }

    public static TempAssetFile Create(IAssetFile source)
    {
        return new TempAssetFile(source.FileName, source.MimeType);
    }

    public TempAssetFile(string fileName, string mimeType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        FileName = fileName;

        MimeType = mimeType;
    }

    public ValueTask DisposeAsync()
    {
        return stream.DisposeAsync();
    }

    public Stream OpenWrite()
    {
        return new NonDisposingStream(stream);
    }

    public Stream OpenRead()
    {
        return new NonDisposingStream(stream);
    }
}
