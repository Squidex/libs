﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net;

namespace Squidex.Assets.TusClient.Internal;

internal sealed class ProgressableStreamContent(Stream content, int uploadBufferSize, Func<long, Task> uploadProgress) : HttpContent
{
    private readonly long uploadLength = content.Length - content.Position;

    public ProgressableStreamContent(Stream content, Func<long, Task> uploadProgress)
        : this(content, 4096, uploadProgress)
    {
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return SerializeToStreamAsync(stream, default);
    }

#if NET5_0_OR_GREATER
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context,
        CancellationToken cancellationToken)
    {
        return SerializeToStreamAsync(stream, cancellationToken);
    }

    private async Task SerializeToStreamAsync(Stream stream,
        CancellationToken ct)
    {
        var buffer = new byte[uploadBufferSize].AsMemory();

        while (true)
        {
            var bytesRead = await content.ReadAsync(buffer, ct);

            if (bytesRead <= 0)
            {
                break;
            }

            await stream.WriteAsync(buffer[..bytesRead], ct);

            await uploadProgress(content.Position);
        }
    }
#else
    private async Task SerializeToStreamAsync(Stream stream,
        CancellationToken ct)
    {
        var buffer = new byte[uploadBufferSize];

        while (true)
        {
            var bytesRead = await content.ReadAsync(buffer, 0, buffer.Length, ct);

            if (bytesRead <= 0)
            {
                break;
            }

            await stream.WriteAsync(buffer, 0, bytesRead, ct);

            await uploadProgress(content.Position);
        }
    }
#endif
    protected override bool TryComputeLength(out long length)
    {
        length = uploadLength;

        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            content.Dispose();
        }

        base.Dispose(disposing);
    }
}
