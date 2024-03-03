// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.Internal;

namespace Squidex.Assets;

public abstract class AssetFile : IDisposable, IAsyncDisposable
{
    private readonly string fileName;
    private readonly string mimeType;
    private readonly long fileSize;

    public virtual string FileName => fileName;

    public virtual string MimeType => mimeType;

    public virtual long FileSize => fileSize;

    protected AssetFile(string fileName, string mimeType, long fileSize)
    {
        Guard.NotNullOrEmpty(fileName, nameof(fileName));
        Guard.NotNullOrEmpty(mimeType, nameof(mimeType));
        Guard.GreaterEquals(fileSize, 0, nameof(fileSize));

        this.fileName = fileName;
        this.fileSize = fileSize;
        this.mimeType = mimeType;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        return;
    }

    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return default;
    }

    public abstract Stream OpenRead();
}
