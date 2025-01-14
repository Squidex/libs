// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Squidex.Assets.ImageMagick.Internal;

namespace Squidex.Assets.ImageMagick;

public sealed class ImageMagickThumbnailGenerator : AssetThumbnailGeneratorBase
{
    public override bool CanComputeBlurHash()
    {
        return false;
    }

    protected override Task<string?> ComputeBlurHashCoreAsync(Stream source, string mimeType, BlurOptions options,
        CancellationToken ct = default)
    {
        return Task.FromResult<string?>(null);
    }

    protected override async Task CreateThumbnailCoreAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
        CancellationToken ct = default)
    {
        var w = options.TargetWidth ?? 0;
        var h = options.TargetHeight ?? 0;

        using (var collection = new MagickImageCollection())
        {
            await collection.ReadAsync(source, GetFormat(mimeType), ct);

            var firstImage = collection[0];
            var firstFormat = firstImage.Format;

            var targetFormat = options.GetFormat(firstFormat);
            var targetFormatInfo = MagickFormatInfo.Create(targetFormat);

            collection.Coalesce();

            var images =
                targetFormatInfo?.SupportsMultipleFrames == true ?
                collection :
                collection.Take(1);

            foreach (var image in images)
            {
                var clone = image.Clone();
                var color = options.ParseColor();
                if (w > 0 || h > 0)
                {
                    var resizeMode = GetResizeMode(options, w, h, image);
                    var resizeAnchor = GetResizeAnchor(options);

                    var (size, pad) = ResizeHelper.CalculateTargetLocationAndBounds(
                        resizeMode,
                        new Size((int)image.Width, (int)image.Height),
                        w,
                        h,
                        resizeAnchor);

                    var sourceRectangle = new MagickGeometry((uint)pad.Width, (uint)pad.Height)
                    {
                        IgnoreAspectRatio = true
                    };

                    clone.Resize(sourceRectangle);

                    image.Extent((uint)size.Width, (uint)size.Height);
                    image.CompositeClear(color);
                    image.Composite(clone, pad.X, pad.Y, CompositeOperator.Over);
                }
                else
                {
                    image.CompositeClear(color);
                    image.Composite(clone, 0, 0, CompositeOperator.Over);
                }

                image.AutoOrient();

                if (options.Quality.HasValue)
                {
                    image.Quality = (uint)options.Quality.Value;
                }
            }

            if (targetFormatInfo?.SupportsMultipleFrames == true)
            {
                await collection.WriteAsync(destination, targetFormat, ct);
            }
            else
            {
                await collection[0].WriteAsync(destination, targetFormat, ct);
            }
        }
    }

    protected override async Task FixCoreAsync(Stream source, string mimeType, Stream destination,
        CancellationToken ct = default)
    {
        using (var collection = new MagickImageCollection())
        {
            await collection.ReadAsync(source, GetFormat(mimeType), ct);

            collection.Coalesce();

            foreach (var image in collection)
            {
                image.AutoOrient();
                image.RemoveAllProfiles();
            }

            await collection.WriteAsync(destination, ct);
        }
    }

    protected override Task<ImageInfo?> GetImageInfoCoreAsync(Stream source, string mimeType,
        CancellationToken ct = default)
    {
        try
        {
            using (var image = new MagickImage())
            {
                image.Ping(source, new MagickReadSettings
                {
                    Format = GetFormat(mimeType)
                });

                var hasSensitiveMetadata =
                    image.ProfileNames.Any();

                return Task.FromResult<ImageInfo?>(new ImageInfo(
                    image.Format.ToImageFormat(),
                    (int)image.Width,
                    (int)image.Height,
                    image.Orientation.GetOrientation(),
                    hasSensitiveMetadata));
            }
        }
        catch
        {
            return Task.FromResult<ImageInfo?>(null);
        }
    }

    private static PointF? GetResizeAnchor(ResizeOptions options)
    {
        PointF? centerCoordinates = null;

        if (options.FocusX.HasValue && options.FocusY.HasValue)
        {
            centerCoordinates = new PointF(
                +(options.FocusX.Value / 2f) + 0.5f,
                -(options.FocusY.Value / 2f) + 0.5f
            );
        }

        return centerCoordinates;
    }

    private static ResizeMode GetResizeMode(ResizeOptions options, int w, int h, IMagickImage<byte> image)
    {
        var isCropUpsize = options.Mode == ResizeMode.CropUpsize;

        var resizeMode = options.Mode;
        if (isCropUpsize)
        {
            resizeMode = ResizeMode.Crop;
        }

        if (w >= image.Width && h >= image.Height && resizeMode == ResizeMode.Crop && !isCropUpsize)
        {
            resizeMode = ResizeMode.BoxPad;
        }

        return resizeMode;
    }

    private static MagickFormat GetFormat(string mimeType)
    {
        var format = MagickFormat.Unknown;

        if (string.Equals(mimeType, "image/x-tga", StringComparison.OrdinalIgnoreCase))
        {
            format = MagickFormat.Tga;
        }
        else if (string.Equals(mimeType, "image/avif", StringComparison.OrdinalIgnoreCase))
        {
            format = MagickFormat.Avif;
        }
        else if (string.Equals(mimeType, "image/bmp", StringComparison.OrdinalIgnoreCase))
        {
            format = MagickFormat.Bmp;
        }

        return format;
    }
}
