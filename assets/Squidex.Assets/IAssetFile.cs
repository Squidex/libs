// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public interface IAssetFile : IAsyncDisposable
{
    long FileSize { get; }

    string FileName { get; }

    string MimeType { get; }

    ValueTask<Stream> OpenReadAsync(
        CancellationToken ct = default);
}
