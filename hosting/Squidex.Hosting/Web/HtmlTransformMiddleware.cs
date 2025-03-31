// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;

namespace Squidex.Hosting.Web;

public sealed class HtmlTransformMiddleware(HtmlTransformOptions options, RequestDelegate next)
{
    private sealed class BufferStream(HttpContext context) : Stream
    {
        private static readonly RecyclableMemoryStreamManager Buffers = new RecyclableMemoryStreamManager();
        private readonly Stream originalBody = context.Response.Body;
        private bool writingStarted;
        private MemoryStream? memoryStream;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => ActualStream.Length;

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        private Stream ActualStream
        {
            get => memoryStream ?? originalBody;
        }

        public async Task CompleteAsync(HtmlTransformOptions options)
        {
            if (memoryStream == null || memoryStream.Length == 0)
            {
                return;
            }

            try
            {
                var html = Encoding.UTF8.GetString(memoryStream.ToArray());

                if (options.AdjustBase)
                {
                    html = html.AdjustBase(context);
                }

                if (options.Transform != null)
                {
                    html = await options.Transform(html, context);
                }

                var bytes = Encoding.UTF8.GetBytes(html);

                if (!context.Response.HasStarted)
                {
                    context.Response.ContentLength = bytes.Length;
                }

                await originalBody.WriteAsync(bytes, context.RequestAborted);
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Return the buffer stream back to the pool.
            memoryStream?.Dispose();
        }

        private void EnsureBuffer()
        {
            var isWritingStarted = writingStarted;

            // Fast exit point from this method.
            writingStarted = true;

            // This is the only safe way to check when we start to write to the body.
            if (isWritingStarted)
            {
                return;
            }

            if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) != true)
            {
                return;
            }

            // Use a pool to reduce memory allocations.
            memoryStream = Buffers.GetStream();
        }

        public override Task FlushAsync(
            CancellationToken cancellationToken)
        {
            EnsureBuffer();

            return ActualStream.FlushAsync(cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            EnsureBuffer();

            return ActualStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            EnsureBuffer();

            return ActualStream.WriteAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using (var bufferStream = new BufferStream(context))
        {
            context.Response.Body = bufferStream;

            await next(context);

            await bufferStream.CompleteAsync(options);
        }
    }
}
