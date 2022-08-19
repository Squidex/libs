// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public sealed class NonDisposingStream : DelegateStream
    {
        public NonDisposingStream(Stream inner)
            : base(inner)
        {
            inner.Position = 0;
        }

        public override void Close()
        {
            Flush();
        }

        protected override void Dispose(bool disposing)
        {
            Flush();
        }

        public override async ValueTask DisposeAsync()
        {
            await FlushAsync();
        }
    }
}
