// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Squidex.Hosting.Web;

public sealed class HtmlTransformMiddleware
{
    private readonly HtmlTransformOptions options;
    private readonly RequestDelegate next;

    public HtmlTransformMiddleware(HtmlTransformOptions options, RequestDelegate next)
    {
        this.options = options;
        this.next = next;
    }

    private sealed class BufferStream : Stream
    {
        private readonly Stream inner;
        private MemoryStream? memoryStream;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => ActualStream.Length;

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        private Stream ActualStream
        {
            get => memoryStream ?? inner;
        }

        public BufferStream(Stream inner)
        {
            this.inner = inner;
        }

        public void Buffer()
        {
            memoryStream = new MemoryStream();
        }

        public async Task CompleteAsync(HtmlTransformOptions options, HttpContext context,
            CancellationToken cancellationToken)
        {
            if (memoryStream != null)
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

                await inner.WriteAsync(bytes, cancellationToken);

                memoryStream = new MemoryStream();
            }
            else
            {
                await inner.FlushAsync(cancellationToken);
            }
        }

        public override Task FlushAsync(
            CancellationToken cancellationToken)
        {
            return ActualStream.FlushAsync(cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            return ActualStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
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
        var response = context.Response;

        var bufferStream = new BufferStream(response.Body);

        response.Body = bufferStream;
        response.OnStarting(() =>
        {
            if (response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) != true)
            {
                return Task.CompletedTask;
            }

            bufferStream.Buffer();
            return Task.CompletedTask;
        });

        await next(context);

        if (bufferStream.Length == 0 || response.StatusCode == StatusCodes.Status304NotModified)
        {
            return;
        }

        await bufferStream.CompleteAsync(options, context, context.RequestAborted);
    }
}
