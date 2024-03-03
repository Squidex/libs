// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public sealed class TempAssetFile : AssetFile
{
    private readonly Stream stream;

    public override long FileSize => stream.Length;

    public static TempAssetFile Create(AssetFile source)
    {
        return new TempAssetFile(source.FileName, source.MimeType);
    }

    public TempAssetFile(string fileName, string mimeType)
        : base(fileName, mimeType, 0)
    {
        stream = TempHelper.GetTempStream();
    }

    public override void Dispose()
    {
        stream.Dispose();
    }

    public override ValueTask DisposeAsync()
    {
        return stream.DisposeAsync();
    }

    public Stream OpenWrite()
    {
        return new NonDisposingStream(stream);
    }

    public override Stream OpenRead()
    {
        return new NonDisposingStream(stream);
    }
}
