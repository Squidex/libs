// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ImageSharpMode = SixLabors.ImageSharp.Processing.ResizeMode;
using ImageSharpOptions = SixLabors.ImageSharp.Processing.ResizeOptions;

namespace Squidex.Assets.Internal;

internal static class Extensions
{
    public static IImageEncoder GetEncoder(this ResizeOptions options, IImageFormat? format)
    {
        var imageFormatsManager = Configuration.Default.ImageFormatsManager;

        if (options.Format != null)
        {
            if (imageFormatsManager.TryFindFormatByMimeType(options.Format.Value.ToMimeType(), out var found) && found != null)
            {
                format = found;
            }
        }

        if (format == null)
        {
            throw new NotSupportedException();
        }

        var encoder = imageFormatsManager.GetEncoder(format) ?? throw new NotSupportedException();
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

    public static void Watermark(this IImageProcessingContext operation, Image watermark, WatermarkAnchor anchor, float opacity)
    {
        var size = operation.GetCurrentSize();

        if (watermark.Size.Width > size.Width || watermark.Size.Height > size.Height)
        {
            watermark.Mutate(watermarkOperation =>
            {
                var options = new ImageSharpOptions { Size = size, Mode = ImageSharpMode.Max };

                watermarkOperation.Resize(options);
            });
        }

        var x = 0;
        var y = 0;
        if (anchor is WatermarkAnchor.TopRight or WatermarkAnchor.BottomRight)
        {
            x = size.Width - watermark.Width;
        }

        if (anchor is WatermarkAnchor.BottomLeft or WatermarkAnchor.BottomRight)
        {
            y = size.Height - watermark.Height;
        }

        if (anchor == WatermarkAnchor.Center)
        {
            x = (size.Width - watermark.Width) / 2;
            y = (size.Height - watermark.Height) / 2;
        }

        operation.DrawImage(watermark, new Point(x, y), opacity);
    }

    public static async Task<Image> GetImageAsync(this IHttpClientFactory httpClientFactory, string url,
        CancellationToken ct)
    {
        using var httpClient = httpClientFactory.CreateClient();
        using var httpResponse = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

        await using var httpStream = await httpResponse.Content.ReadAsStreamAsync(ct);
        return await Image.LoadAsync(httpStream, ct);
    }
}
