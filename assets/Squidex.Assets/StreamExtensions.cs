// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Buffers;

namespace Squidex.Assets;

public static class StreamExtensions
{
    private const int BufferSize = 81920;

    public static async Task CopyToAsync(this Stream source, Stream target, BytesRange range,
        CancellationToken ct, bool skip = true)
    {
        // The shared pool is per core and lock free, a custom pool would be slower for no benefit.
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            if (skip && range.From > 0)
            {
                source.Seek(range.From.Value, SeekOrigin.Begin);
            }

            var bytesLeft = range.Length;

            while (true)
            {
                if (bytesLeft <= 0)
                {
                    return;
                }

                ct.ThrowIfCancellationRequested();

                var readLength = (int)Math.Min(buffer.Length, bytesLeft);
                var readBytes = await source.ReadAsync(buffer.AsMemory(0, readLength), ct);

                bytesLeft -= readBytes;

                if (readBytes == 0)
                {
                    return;
                }

                ct.ThrowIfCancellationRequested();

                await target.WriteAsync(buffer.AsMemory(0, readBytes), ct);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static long GetLengthOrZero(this Stream stream)
    {
        try
        {
            return stream.Length;
        }
        catch
        {
            return 0;
        }
    }
}
