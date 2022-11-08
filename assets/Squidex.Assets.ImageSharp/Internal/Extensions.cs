// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace Squidex.Assets.Internal;

internal static class Extensions
{
    public static IImageEncoder GetEncoder(this ResizeOptions options, IImageFormat format)
    {
        var imageFormatsManager = Configuration.Default.ImageFormatsManager;

        if (options.Format != null)
        {
            format = imageFormatsManager.FindFormatByMimeType(options.Format.Value.ToMimeType()) ?? format;
        }

        var encoder = imageFormatsManager.FindEncoder(format);

        if (encoder == null)
        {
            throw new NotSupportedException();
        }

        if (encoder is PngEncoder png && png.ColorType != PngColorType.RgbWithAlpha)
        {
            encoder = new PngEncoder
            {
                ColorType = PngColorType.RgbWithAlpha
            };
        }

        var quality = options.Quality ?? 80;

        if (encoder is JpegEncoder jpg && jpg.Quality != quality)
        {
            encoder = new JpegEncoder
            {
                Quality = quality
            };
        }

        if (encoder is WebpEncoder webp && webp.Quality != quality)
        {
            encoder = new WebpEncoder
            {
                Quality = quality
            };
        }

        return encoder;
    }
}
