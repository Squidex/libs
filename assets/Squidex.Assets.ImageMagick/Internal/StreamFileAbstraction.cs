// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.IO;
using static TagLib.File;

namespace Squidex.Assets.Internal
{
    internal sealed class StreamFileAbstraction : IFileAbstraction
    {
        private readonly Stream stream;
        private readonly string extension;

        public string Name => $"image.{extension}";

        public Stream ReadStream => stream;

        public Stream WriteStream => throw new NotSupportedException();

        public StreamFileAbstraction(Stream stream, string extension)
        {
            this.stream = stream;
            this.extension = extension;
        }

        public void CloseStream(Stream stream)
        {
        }
    }
}
