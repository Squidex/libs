// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public static class ImageExtensions
{
    public static ImageFormat? ToImageFormat(this string mimeType)
    {
        switch (mimeType?.ToLowerInvariant())
        {
            case "image/avif":
                return ImageFormat.AVIF;
            case "image/bmp":
                return ImageFormat.BMP;
            case "image/gif":
                return ImageFormat.GIF;
            case "image/jpeg":
                return ImageFormat.JPEG;
            case "image/png":
                return ImageFormat.PNG;
            case "image/x-tga":
                return ImageFormat.TGA;
            case "image/tiff":
                return ImageFormat.TIFF;
            case "image/webp":
                return ImageFormat.WEBP;
            default:
                return null;
        }
    }

    public static string ToMimeType(this ImageFormat format)
    {
        switch (format)
        {
            case ImageFormat.AVIF:
                return "image/avif";
            case ImageFormat.BMP:
                return "image/bmp";
            case ImageFormat.GIF:
                return "image/gif";
            case ImageFormat.JPEG:
                return "image/jpeg";
            case ImageFormat.PNG:
                return "image/png";
            case ImageFormat.TGA:
                return "image/x-tga";
            case ImageFormat.TIFF:
                return "image/tiff";
            case ImageFormat.WEBP:
                return "image/webp";
            default:
                throw new ArgumentException("Invalid format.", nameof(format));
        }
    }
}
