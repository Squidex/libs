// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.ImageSharp;

namespace Squidex.Assets;

public class ImageSharpThumbnailGeneratorTests : AssetThumbnailGeneratorTests
{
    protected override HashSet<ImageFormat> SupportedFormats =>
    [
        ImageFormat.BMP,
        ImageFormat.PNG,
        ImageFormat.GIF,
        ImageFormat.JPEG,
        ImageFormat.TGA,
        ImageFormat.TIFF,
        ImageFormat.WEBP,
    ];

    protected override string Name()
    {
        return "imagesharp";
    }

    protected override IAssetThumbnailGenerator CreateSut()
    {
        var httpClientFactory =
            new ServiceCollection()
                .AddHttpClient()
                .BuildServiceProvider()
                .GetRequiredService<IHttpClientFactory>();

        return new ImageSharpThumbnailGenerator(httpClientFactory);
    }

    [Fact]
    public void Should_not_be_resizable_if_format_not_supported2()
    {
        var result = sut.IsResizable("image/png", new ResizeOptions { Format = ImageFormat.AVIF }, out var destimationMimeType);

        Assert.False(result);
        Assert.Null(destimationMimeType);
    }

    [Theory]
    [InlineData(WatermarkAnchor.TopLeft)]
    [InlineData(WatermarkAnchor.TopRight)]
    [InlineData(WatermarkAnchor.BottomLeft)]
    [InlineData(WatermarkAnchor.BottomRight)]
    [InlineData(WatermarkAnchor.Center)]
    public async Task Should_add_watermark(WatermarkAnchor anchor)
    {
        var (mimeType, source) = GetImage("landscape.png");

        await using (var target = GetStream($"watermark_{anchor}"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                WatermarkAnchor = anchor,
                WatermarkUrl = "https://github.com/Squidex/squidex/blob/master/media/logo-wide.png?raw=true",
            });

            Assert.True(target.Length > source.Length);
        }
    }

    [Theory]
    [InlineData(WatermarkAnchor.TopLeft)]
    [InlineData(WatermarkAnchor.TopRight)]
    [InlineData(WatermarkAnchor.BottomLeft)]
    [InlineData(WatermarkAnchor.BottomRight)]
    [InlineData(WatermarkAnchor.Center)]
    public async Task Should_add_watermark_to_small_image(WatermarkAnchor anchor)
    {
        var (mimeType, source) = GetImage("landscape_small.png");

        await using (var target = GetStream($"watermark_small_{anchor}"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                WatermarkAnchor = anchor,
                WatermarkUrl = "https://github.com/Squidex/squidex/blob/master/media/logo-wide.png?raw=true",
            });

            Assert.True(target.Length > source.Length);
        }
    }
}
