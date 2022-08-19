// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'

namespace Squidex.Assets.Internal
{
    internal sealed class CancellableStream : DelegateStream
    {
        private readonly CancellationToken cancellationToken;

        public override long Length
        {
            get => throw new NotSupportedException();
        }

        public override bool CanWrite
        {
            get => false;
        }

        public CancellableStream(Stream innerStream,
            CancellationToken cancellationToken)
            : base(innerStream)
        {
            this.cancellationToken = cancellationToken;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }

            try
            {
                return base.Read(buffer, offset, count);
            }
            catch
            {
                // Very ugly because it is not clear which exception is thrown here when the request is aborted. Also depends on Server (Kestrel, IIS).
                return 0;
            }
        }

        public override int Read(Span<byte> buffer)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }

            try
            {
                return base.Read(buffer);
            }
            catch
            {
                return 0;
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }

            try
            {
                return await base.ReadAsync(buffer, offset, count, default);
            }
            catch
            {
                return 0;
            }
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }

            try
            {
                return await base.ReadAsync(buffer, default);
            }
            catch
            {
                return 0;
            }
        }

        public override int ReadByte()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }

            try
            {
                return base.ReadByte();
            }
            catch
            {
                return 0;
            }
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            try
            {
                base.CopyTo(destination, bufferSize);
            }
            catch
            {
                return;
            }
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize,
            CancellationToken cancellationToken)
        {
            try
            {
                await base.CopyToAsync(destination, bufferSize, cancellationToken);
            }
            catch
            {
                return;
            }
        }
    }
}
