// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public abstract class DelegateStream : Stream
    {
        private readonly Stream innerStream;

        public override bool CanRead
        {
            get => innerStream.CanRead;
        }

        public override bool CanSeek
        {
            get => innerStream.CanSeek;
        }

        public override bool CanWrite
        {
            get => innerStream.CanWrite;
        }

        public override bool CanTimeout
        {
            get => innerStream.CanTimeout;
        }

        public override long Length
        {
            get => innerStream.Length;
        }

        public override long Position
        {
            get => innerStream.Position;
            set => innerStream.Position = value;
        }

        public override int ReadTimeout
        {
            get => innerStream.ReadTimeout;
            set => innerStream.ReadTimeout = value;
        }

        public override int WriteTimeout
        {
            get => innerStream.WriteTimeout;
            set => innerStream.WriteTimeout = value;
        }

        protected DelegateStream(Stream innerStream)
        {
            if (innerStream == Null)
            {
                throw new ArgumentNullException(nameof(innerStream));
            }

            this.innerStream = innerStream;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                innerStream.Dispose();
            }

            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return innerStream.DisposeAsync();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return innerStream.Seek(offset, origin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return innerStream.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            return innerStream.Read(buffer);
        }

        public override int ReadByte()
        {
            return innerStream.ReadByte();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            return innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            return innerStream.ReadAsync(buffer, cancellationToken);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return innerStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return innerStream.EndRead(asyncResult);
        }

        public override void Flush()
        {
            innerStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return innerStream.FlushAsync(cancellationToken);
        }

        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            innerStream.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            innerStream.Write(buffer);
        }

        public override void WriteByte(byte value)
        {
            innerStream.WriteByte(value);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            return innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            return innerStream.WriteAsync(buffer, cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return innerStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            innerStream.EndWrite(asyncResult);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize,
            CancellationToken cancellationToken)
        {
            return innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }
    }
}
