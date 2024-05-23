// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public class DelegateAssetFile : IAssetFile
{
    private readonly Func<Stream> openStream;

    public string FileName { get; }

    public string MimeType { get; }

    public long FileSize { get; }

    public DelegateAssetFile(string fileName, string mimeType, long fileSize,
        Func<Stream> openStream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentOutOfRangeException.ThrowIfLessThan(fileSize, 0);

        FileName = fileName;
        FileSize = fileSize;
        MimeType = mimeType;

        this.openStream = openStream;
    }

    public Stream OpenRead()
    {
        return openStream();
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return default;
    }
}
