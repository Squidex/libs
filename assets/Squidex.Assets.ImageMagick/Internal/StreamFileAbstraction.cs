// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.IO;
using static TagLib.File;

namespace Squidex.Assets.Internal;

internal sealed class StreamFileAbstraction(Stream stream, string extension) : IFileAbstraction
{
    public string Name => $"image.{extension}";

    public Stream ReadStream => stream;

    public Stream WriteStream => throw new NotSupportedException();

    public void CloseStream(Stream stream)
    {
    }
}
