// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Assets;

public class ImageSharpThumbnailGeneratorTests : AssetThumbnailGeneratorTests
{
    protected override HashSet<ImageFormat> SupportedFormats => new HashSet<ImageFormat>
    {
        ImageFormat.BMP,
        ImageFormat.PNG,
        ImageFormat.GIF,
        ImageFormat.JPEG,
        ImageFormat.TGA,
        ImageFormat.TIFF,
        ImageFormat.WEBP
    };

    protected override string Name()
    {
        return "imagesharp";
    }

    protected override IAssetThumbnailGenerator CreateSut()
    {
        return new ImageSharpThumbnailGenerator();
    }

    [Fact]
    public void Should_not_be_resizable_if_format_not_supported2()
    {
        var result = sut.IsResizable("image/png", new ResizeOptions { Format = ImageFormat.AVIF }, out var destimationMimeType);

        Assert.False(result);
        Assert.Null(destimationMimeType);
    }
}
