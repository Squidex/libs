// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Blurhash.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Squidex.Assets.ImageSharp.Internal;
using ImageSharpInfo = SixLabors.ImageSharp.ImageInfo;
using ImageSharpMode = SixLabors.ImageSharp.Processing.ResizeMode;
using ImageSharpOptions = SixLabors.ImageSharp.Processing.ResizeOptions;

namespace Squidex.Assets.ImageSharp;

public sealed class ImageSharpThumbnailGenerator(IHttpClientFactory httpClientFactory) : AssetThumbnailGeneratorBase
{
    private readonly HashSet<string> mimeTypes = Configuration.Default.ImageFormatsManager.ImageFormats.SelectMany(x => x.MimeTypes).ToHashSet();

    public override bool CanReadAndWrite(string mimeType)
    {
        return mimeType != null && mimeTypes.Contains(mimeType);
    }

    public override bool CanComputeBlurHash()
    {
        return true;
    }

    protected override async Task<string?> ComputeBlurHashCoreAsync(Stream source, string mimeType, BlurOptions options,
        CancellationToken ct = default)
    {
        try
        {
            using (var image = await Image.LoadAsync<Rgb24>(source, ct))
            {
                return Blurhasher.Encode(image, options.ComponentX, options.ComponentY);
            }
        }
        catch
        {
            return null;
        }
    }

    protected override async Task CreateThumbnailCoreAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
        CancellationToken ct = default)
    {
        var w = options.TargetWidth ?? 0;
        var h = options.TargetHeight ?? 0;

        using (var image = await Image.LoadAsync(source, ct))
        {
            var watermark = await GetWatermarkAsync(options, ct);

            image.Mutate(operation =>
            {
                operation.AutoOrient();

                if (w > 0 || h > 0)
                {
                    var resizeMode = GetResizeMode(options, w, h, image);
                    var bgColor = options.Background != null && Color.TryParse(options.Background, out var color) ? color : Color.Transparent;
                    var resizeOptions = new ImageSharpOptions { Size = new Size(w, h), Mode = resizeMode, PremultiplyAlpha = true, PadColor = bgColor };

                    if (options.FocusX.HasValue && options.FocusY.HasValue)
                    {
                        resizeOptions.CenterCoordinates = new PointF(
                            +(options.FocusX.Value / 2f) + 0.5f,
                            -(options.FocusY.Value / 2f) + 0.5f
                        );
                    }

                    operation.Resize(resizeOptions);

                    operation.BackgroundColor(bgColor);
                }

                if (watermark != null)
                {
                    operation.Watermark(watermark, options.WatermarkAnchor, options.WatermarkOpacity);
                }
            });

            var encoder = options.GetEncoder(image.Metadata.DecodedImageFormat);

            await image.SaveAsync(destination, encoder, ct);
        }
    }

    private static ImageSharpMode GetResizeMode(ResizeOptions options, int w, int h, Image image)
    {
        var isCropUpsize = options.Mode == ResizeMode.CropUpsize;

        if (!Enum.TryParse<ImageSharpMode>(options.Mode.ToString(), true, out var resizeMode))
        {
            resizeMode = ImageSharpMode.Max;
        }

        if (isCropUpsize)
        {
            resizeMode = ImageSharpMode.Crop;
        }

        if (w >= image.Width && h >= image.Height && resizeMode == ImageSharpMode.Crop && !isCropUpsize)
        {
            resizeMode = ImageSharpMode.BoxPad;
        }

        return resizeMode;
    }

    protected override async Task<ImageInfo?> GetImageInfoCoreAsync(Stream source, string mimeType,
        CancellationToken ct = default)
    {
        try
        {
            var imageInfo = await Image.IdentifyAsync(source, ct);

            if (imageInfo?.Metadata.DecodedImageFormat == null)
            {
                return null;
            }

            return GetImageInfo(imageInfo);
        }
        catch
        {
            return null;
        }
    }

    protected override async Task FixCoreAsync(Stream source, string mimeType, Stream destination,
        CancellationToken ct = default)
    {
        using (var image = await Image.LoadAsync(source, ct))
        {
            if (image.Metadata.DecodedImageFormat == null)
            {
                throw new NotSupportedException();
            }

            var encoder = Configuration.Default.ImageFormatsManager.GetEncoder(image.Metadata.DecodedImageFormat) ?? throw new NotSupportedException();

            image.Mutate(x => x.AutoOrient());
            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            await image.SaveAsync(destination, encoder, ct);
        }
    }

    private async Task<Image?> GetWatermarkAsync(ResizeOptions options,
        CancellationToken ct)
    {
        if (options.WatermarkUrl == null)
        {
            return null;
        }

        try
        {
            return await httpClientFactory.GetImageAsync(options.WatermarkUrl, ct);
        }
        catch
        {
            return null;
        }
    }

    private static ImageInfo GetImageInfo(ImageSharpInfo imageInfo)
    {
        var orientation = ImageOrientation.None;

        if (imageInfo.Metadata.ExifProfile?.TryGetValue(ExifTag.Orientation, out var tag) == true)
        {
            orientation = (ImageOrientation)tag.Value;
        }

        var format = ImageFormat.PNG;

        switch (imageInfo.Metadata.DecodedImageFormat)
        {
            case BmpFormat:
                format = ImageFormat.BMP;
                break;
            case JpegFormat:
                format = ImageFormat.JPEG;
                break;
            case TgaFormat:
                format = ImageFormat.TGA;
                break;
            case TiffFormat:
                format = ImageFormat.TIFF;
                break;
            case GifFormat:
                format = ImageFormat.GIF;
                break;
            case WebpFormat:
                format = ImageFormat.WEBP;
                break;
        }

        var hasSensitiveMetadata =
            imageInfo.Metadata.ExifProfile?.Values.Count > 0 ||
            imageInfo.Metadata.IccProfile?.Entries.Length > 0 ||
            imageInfo.Metadata.IptcProfile?.Values.Any() == true ||
            imageInfo.Metadata.XmpProfile != null;

        return new ImageInfo(format, imageInfo.Width, imageInfo.Height, orientation, hasSensitiveMetadata);
    }
}
