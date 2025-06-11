// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Squidex.Assets;

public abstract class AssetThumbnailGeneratorTests
{
#pragma warning disable SA1401 // Fields should be private
    protected readonly IAssetThumbnailGenerator sut;
#pragma warning restore SA1401 // Fields should be private

    public static TheoryData<ImageFormat, ImageFormat> GetConversions()
    {
        var result = new TheoryData<ImageFormat, ImageFormat>();

        var allFormats = Enum.GetValues(typeof(ImageFormat)).OfType<ImageFormat>();

        foreach (var source in allFormats)
        {
            foreach (var target in allFormats)
            {
                if (!Equals(target, source))
                {
                    result.Add(target, source);
                }
            }
        }

        return result;
    }

    protected AssetThumbnailGeneratorTests()
    {
#pragma warning disable RECS0021 // Warns about calls to virtual member functions occuring in the constructor
#pragma warning disable MA0056 // Do not call overridable members in constructor
        sut = CreateSut();
#pragma warning restore MA0056 // Do not call overridable members in constructor
#pragma warning restore RECS0021 // Warns about calls to virtual member functions occuring in the constructor
    }

    protected abstract IAssetThumbnailGenerator CreateSut();

    protected abstract string Name();

    protected virtual HashSet<ImageFormat> SupportedFormats => Enum.GetValues<ImageFormat>().ToHashSet();

    protected virtual bool SupportsBlurHash => true;

    [Theory]
    [MemberData(nameof(GetConversions))]
    public async Task Should_convert_between_formats(ImageFormat sourceFormat, ImageFormat targetFormat)
    {
        if (SupportedFormats?.Contains(sourceFormat) == false)
        {
            return;
        }

        if (SupportedFormats?.Contains(targetFormat) == false)
        {
            return;
        }

        var (mimeType, source) = GetImage(sourceFormat);

        await using (var target = GetStream($"transform.{sourceFormat.ToString().ToLowerInvariant()}", targetFormat.ToString().ToLowerInvariant()))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions { Format = targetFormat });

            target.Position = 0;

            var imageInfo = await sut.GetImageInfoAsync(target, targetFormat.ToMimeType()!);

            Assert.Equal(targetFormat, imageInfo?.Format);
        }
    }

    [Fact]
    public async Task Should_return_same_image_if_no_size_and_quality_is_passed_for_thumbnail()
    {
        var (mimeType, source) = GetImage(ImageFormat.PNG);

        await using (var target = GetStream("resize-copy"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions());

            Assert.Equal(target.Length, source.Length);
        }
    }

    [Fact]
    public async Task Should_return_same_image_if_no_target_format_is_same_as_source_type()
    {
        var (mimeType, source) = GetImage(ImageFormat.PNG);

        await using (var target = GetStream("resize-copy"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions { Format = ImageFormat.PNG });

            Assert.Equal(target.Length, source.Length);
        }
    }

    [Fact]
    public async Task Should_upsize_image_to_target()
    {
        var (mimeType, source) = GetImage(ImageFormat.PNG);

        await using (var target = GetStream("upsize"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                TargetWidth = 1000,
                TargetHeight = 1000,
                Mode = ResizeMode.Stretch,
            });

            Assert.True(target.Length > source.Length);
        }
    }

    [Fact]
    public async Task Should_downsize_image_to_target()
    {
        var (mimeType, source) = GetImage(ImageFormat.PNG);

        await using (var target = GetStream("downsize"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                TargetWidth = 100,
                TargetHeight = 100,
                Mode = ResizeMode.Stretch,
            });

            Assert.True(target.Length < source.Length);
        }
    }

    [Fact]
    public async Task Should_change_jpeg_quality_and_write_to_target()
    {
        var (mimeType, source) = GetImage(ImageFormat.JPEG);

        await using (var target = GetStream("quality", "jpg"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                Quality = 10,
            });

            Assert.True(target.Length < source.Length);
        }
    }

    [Fact]
    public async Task Should_change_png_quality_and_write_to_target()
    {
        var (mimeType, source) = GetImage(ImageFormat.PNG);

        await using (var target = GetStream("quality", "png"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                Quality = 10,
                Format = ImageFormat.JPEG,
            });

            Assert.True(target.Length < source.Length);
        }
    }

    [Fact]
    public async Task Should_resize_pad_with_transparent_background()
    {
        await Resize("pad.transparent", null, ResizeMode.Pad);
    }

    [Fact]
    public async Task Should_resize_pad_with_colored_background()
    {
        await Resize("pad.colored", "red", ResizeMode.Pad);
    }

    [Fact]
    public async Task Should_resize_boxpad_with_transparent_background()
    {
        await Resize("boxpad.transparent", null, ResizeMode.BoxPad);
    }

    [Fact]
    public async Task Should_resize_boxpad_with_colored_background()
    {
        await Resize("boxpad.colored", "red", ResizeMode.BoxPad);
    }

    [Fact]
    public void Should_be_resizable_if_resizing()
    {
        var result = sut.IsResizable("image/png", new ResizeOptions { TargetWidth = 100 }, out var destimationMimeType);

        Assert.True(result);
        Assert.Equal("image/png", destimationMimeType);
    }

    [Fact]
    public void Should_be_resizable_if_format_does_not_match()
    {
        var result = sut.IsResizable("image/png", new ResizeOptions { Format = ImageFormat.WEBP }, out var destimationMimeType);

        Assert.True(result);
        Assert.Equal("image/webp", destimationMimeType);
    }

    [Fact]
    public void Should_not_be_resizable_if_format_does_match()
    {
        var result = sut.IsResizable("image/png", new ResizeOptions { Format = ImageFormat.PNG }, out var destimationMimeType);

        Assert.False(result);
        Assert.Null(destimationMimeType);
    }

    [Fact]
    public void Should_not_be_resizable_if_format_not_supported()
    {
        var result = sut.IsResizable("image/png", new ResizeOptions { Format = (ImageFormat)123 }, out var destimationMimeType);

        Assert.False(result);
        Assert.Null(destimationMimeType);
    }

    [Fact]
    public void Should_not_be_resizable_if_no_format_given()
    {
        var result = sut.IsResizable("image/png", new ResizeOptions(), out var destimationMimeType);

        Assert.False(result);
        Assert.Null(destimationMimeType);
    }

    private async Task Resize(string name, string? color, ResizeMode mode)
    {
        var (mimeType, source) = GetImage("logo.png");

        await using (var target = GetStream(name))
        {
            const int w = 1500;
            const int h = 1500;

            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                Background = color,
                TargetWidth = w,
                TargetHeight = h,
                Mode = mode,
            });

            target.Position = 0;

            var image = await Image.LoadAsync<Rgba32>(target);

            Assert.Equal(w, image.Width);
            Assert.Equal(h, image.Height);

            var expected = Color.Parse(color ?? "transparent").ToPixel<Rgba32>();

            Assert.Equal(expected, image[0, 0]);
            Assert.Equal(expected, image[0, image.Height - 1]);
            Assert.Equal(expected, image[image.Width - 1, 0]);
            Assert.Equal(expected, image[image.Width - 1, image.Height - 1]);
        }
    }

    [Fact]
    public async Task Should_auto_orient_image()
    {
        var (mimeType, source) = GetRotatedJpeg();

        await using (var target = GetStream("oriented", "jpeg"))
        {
            await sut.FixAsync(source, mimeType, target);

            target.Position = 0;

            var imageInfo = await sut.GetImageInfoAsync(target, mimeType);

            Assert.Equal(
                new ImageInfo(
                    ImageFormat.JPEG,
                    PixelWidth: 600,
                    PixelHeight: 135,
                    ImageOrientation.None,
                    false),
                imageInfo!);
        }
    }

    [Fact]
    public async Task Should_resize_landscape_stretch()
    {
        var (mimeType, source) = GetImage("landscape.png");

        await using (var target = GetStream("landscape.stretch"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                TargetWidth = 1000,
                TargetHeight = 200,
                Mode = ResizeMode.Stretch,
            });
        }
    }

    [Fact]
    public async Task Should_resize_landscape_max()
    {
        var (mimeType, source) = GetImage("landscape.png");

        await using (var target = GetStream("landscape.max"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                TargetWidth = 400,
                TargetHeight = 0,
                Mode = ResizeMode.Max,
            });
        }
    }

    [Fact]
    public async Task Should_resize_landscape_min()
    {
        var (mimeType, source) = GetImage("landscape.png");

        await using (var target = GetStream("landscape.min"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                TargetWidth = 100,
                TargetHeight = 0,
                Mode = ResizeMode.Min,
            });
        }
    }

    [Fact]
    public async Task Should_resize_landscape_boxpad()
    {
        var (mimeType, source) = GetImage("landscape.png");

        await using (var target = GetStream("landscape.boxpad"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                TargetWidth = 300,
                TargetHeight = 300,
                Mode = ResizeMode.BoxPad,
            });
        }
    }

    [Fact]
    public async Task Should_resize_landscape_crop()
    {
        var (mimeType, source) = GetImage("landscape.png");

        await using (var target = GetStream("landscape.crop"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                TargetWidth = 100,
                TargetHeight = 100,
                Mode = ResizeMode.Crop,
            });
        }
    }

    [Fact]
    public async Task Should_resize_landscape_crop_upsize()
    {
        var (mimeType, source) = GetImage("landscape.png");

        await using (var target = GetStream("landscape.cropup"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                TargetWidth = 600,
                TargetHeight = 600,
                Mode = ResizeMode.CropUpsize,
            });
        }
    }

    [Fact]
    public async Task Should_resize_landscape_pad()
    {
        var (mimeType, source) = GetImage("landscape.png");

        await using (var target = GetStream("landscape.pad"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                TargetWidth = 50,
                TargetHeight = 0,
                Mode = ResizeMode.Pad,
            });
        }
    }

    [Fact]
    public async Task Should_convert_buggy_image()
    {
        var (mimeType, source) = GetImage("buggy1.jpeg");

        await using (var target = GetStream("buggy1", "webp"))
        {
            await sut.CreateThumbnailAsync(source, mimeType, target, new ResizeOptions
            {
                Format = ImageFormat.WEBP,
            });
        }
    }

    [Fact]
    public async Task Should_return_image_information_if_image_is_valid()
    {
        var (mimeType, source) = GetImage(ImageFormat.PNG);

        var imageInfo = await sut.GetImageInfoAsync(source, mimeType);

        Assert.Equal(
            new ImageInfo(
                ImageFormat.PNG,
                PixelWidth: 600,
                PixelHeight: 600,
                ImageOrientation.None,
                false),
            imageInfo!);
    }

    [Fact]
    public async Task Should_return_image_information_if_rotated()
    {
        var (mimeType, source) = GetRotatedJpeg();

        var imageInfo = await sut.GetImageInfoAsync(source, mimeType);

        Assert.Equal(
            new ImageInfo(
                ImageFormat.JPEG,
                PixelWidth: 135,
                PixelHeight: 600,
                ImageOrientation.LeftBottom,
                true),
            imageInfo!);
    }

    [Fact]
    public async Task Should_compute_blur_hash_from_jpg()
    {
        var (mimeType, source) = GetImage(ImageFormat.JPEG);

        var blurHash = await sut.ComputeBlurHashAsync(source, mimeType, new BlurOptions());

        Assert.True(SupportsBlurHash ? blurHash != null : blurHash == null);
    }

    [Fact]
    public async Task Should_compute_blur_hash_from_png()
    {
        var (mimeType, source) = GetImage(ImageFormat.PNG);

        var blurHash = await sut.ComputeBlurHashAsync(source, mimeType, new BlurOptions());

        Assert.True(SupportsBlurHash ? blurHash != null : blurHash == null);
    }

    [Fact]
    public async Task Should_compute_blur_hash_from_webp()
    {
        var (mimeType, source) = GetImage(ImageFormat.WEBP);

        var blurHash = await sut.ComputeBlurHashAsync(source, mimeType, new BlurOptions());

        Assert.True(SupportsBlurHash ? blurHash != null : blurHash == null);
    }

    [Fact]
    public async Task Should_return_null_if_stream_is_not_an_image()
    {
        var source = new MemoryStream(Convert.FromBase64String("YXNkc2Fk"));

        var imageInfo = await sut.GetImageInfoAsync(source, "binary/plain");

        Assert.Null(imageInfo);
    }

    [Fact]
    public async Task Should_return_null_if_stream_is_an_pdf()
    {
        var (mimeType, source) = GetImage("sample.pdf");

        var imageInfo = await sut.GetImageInfoAsync(source, mimeType);

        Assert.Null(imageInfo);
    }

    protected FileStream GetStream(string type, string? extension = null)
    {
        Directory.CreateDirectory("images");

        return new FileStream($"images/{type}.{Name()}.{extension ?? "png"}", FileMode.Create);
    }

    protected (string, Stream) GetImage(string fileName)
    {
        var filePath = $"Squidex.Assets.Images.{fileName}";
        var fileType = GetMimeType(fileName);

        return (fileType, GetType().Assembly.GetManifestResourceStream(filePath)!);
    }

    protected static string GetMimeType(string fileName)
    {
        var extension = fileName.Split('.')[^1];

        var mimeType = $"image/{extension}";

        if (string.Equals(extension, "tga", StringComparison.OrdinalIgnoreCase))
        {
            mimeType = "image/x-tga";
        }

        return mimeType;
    }

    protected (string, Stream) GetImage(ImageFormat format)
    {
        return GetImage($"logo.{format.ToString().ToLowerInvariant()}");
    }

    protected (string, Stream) GetRotatedJpeg()
    {
        return GetImage("logo-wide-rotated.jpeg");
    }
}
