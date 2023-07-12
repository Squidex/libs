// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using ImageMagick;

namespace Squidex.Assets.Internal;

internal static class Extensions
{
    public static MagickColor ParseColor(this ResizeOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Background))
        {
            return MagickColors.Transparent;
        }

        try
        {
            return new MagickColor(options.Background);
        }
        catch
        {
            return MagickColors.Transparent;
        }
    }

    public static void CompositeClear(this IMagickImage<byte> image, MagickColor color)
    {
        const int BufferSize = 50;

        using (var pixels = image.GetPixels())
        {
            var colorChannels = pixels.Channels;
            var colorArray = color.ToByteArray();

            var buffer = new byte[BufferSize * BufferSize * colorChannels];

            // Create a buffer to reduce native calls and keep memory footprint low.
            for (var x = 0; x < BufferSize; x++)
            {
                for (var y = 0; y < BufferSize; y++)
                {
                    var destination = colorChannels * (x + (y * BufferSize));

                    Array.Copy(colorArray, 0, buffer, destination, colorChannels);
                }
            }

            for (var x = 0; x < image.Width; x += BufferSize)
            {
                for (var y = 0; y < image.Height; y += BufferSize)
                {
                    var w = Math.Min(BufferSize, image.Width - x);
                    var h = Math.Min(BufferSize, image.Height - y);

                    var bufferLength = w * h * colorChannels;

                    var actualBuffer = buffer.AsSpan()[..bufferLength];

                    pixels.SetArea(x, y, w, h, actualBuffer);
                }
            }
        }
    }

    public static MagickFormat GetFormat(this ResizeOptions options, MagickFormat format)
    {
        var result = format;

        switch (options.Format)
        {
            case ImageFormat.AVIF:
                result = MagickFormat.Avif;
                break;
            case ImageFormat.BMP:
                result = MagickFormat.Bmp;
                break;
            case ImageFormat.GIF:
                result = MagickFormat.Gif;
                break;
            case ImageFormat.JPEG:
                result = MagickFormat.Jpeg;
                break;
            case ImageFormat.PNG:
                result = MagickFormat.Png32;
                break;
            case ImageFormat.TGA:
                result = MagickFormat.Tga;
                break;
            case ImageFormat.TIFF:
                result = MagickFormat.Tiff;
                break;
            case ImageFormat.WEBP:
                result = MagickFormat.WebP;
                break;
        }

        switch (result)
        {
            case MagickFormat.Png:
            case MagickFormat.Png00:
            case MagickFormat.Png24:
            case MagickFormat.Png64:
            case MagickFormat.Png8:
                result = MagickFormat.Png32;
                break;
        }

        return result;
    }

    public static IMagickImage<T> RemoveAllProfiles<T>(this IMagickImage<T> image) where T : struct, IConvertible
    {
        foreach (var profileName in image.ProfileNames)
        {
            image.RemoveProfile(profileName);
        }

        return image;
    }

    public static ImageOrientation GetOrientation<T>(this IMagickImage<T> image) where T : struct, IConvertible
    {
        return (ImageOrientation)(image.GetExifProfile()?.GetValue(ExifTag.Orientation)?.Value ?? 0);
    }

    public static ImageOrientation GetOrientation(this OrientationType type)
    {
        return (ImageOrientation)(int)type;
    }

    public static ImageFormat ToImageFormat(this MagickFormat format)
    {
        switch (format)
        {
            case MagickFormat.Avif:
                return ImageFormat.AVIF;
            case MagickFormat.Bmp:
            case MagickFormat.Bmp2:
            case MagickFormat.Bmp3:
                return ImageFormat.BMP;
            case MagickFormat.Gif:
            case MagickFormat.Gif87:
                return ImageFormat.GIF;
            case MagickFormat.Jpeg:
            case MagickFormat.Jpg:
                return ImageFormat.JPEG;
            case MagickFormat.Png:
            case MagickFormat.Png00:
            case MagickFormat.Png24:
            case MagickFormat.Png32:
            case MagickFormat.Png64:
            case MagickFormat.Png8:
                return ImageFormat.PNG;
            case MagickFormat.Tga:
                return ImageFormat.TGA;
            case MagickFormat.Tif:
            case MagickFormat.Tiff:
            case MagickFormat.Tiff64:
                return ImageFormat.TIFF;
            case MagickFormat.WebP:
                return ImageFormat.WEBP;
            default:
                throw new NotSupportedException();
        }
    }
}
