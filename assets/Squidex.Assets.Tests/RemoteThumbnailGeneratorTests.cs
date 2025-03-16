// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.ImageMagick;
using Squidex.Assets.ImageSharp;
using Squidex.Assets.Remote;

namespace Squidex.Assets;

[Trait("Category", "Dependencies")]
public class RemoteThumbnailGeneratorTests : AssetThumbnailGeneratorTests
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
        return "remote";
    }

    protected override IAssetThumbnailGenerator CreateSut()
    {
        var services =
            new ServiceCollection()
                .AddHttpClient("Resize", options =>
                {
                    options.BaseAddress = new Uri("http://localhost:5005");
                }).Services
                .BuildServiceProvider();

        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

        var inner = new CompositeThumbnailGenerator(
        [
            new ImageSharpThumbnailGenerator(httpClientFactory),
            new ImageMagickThumbnailGenerator(),
        ]);

        return new RemoteThumbnailGenerator(httpClientFactory, inner);
    }
}
