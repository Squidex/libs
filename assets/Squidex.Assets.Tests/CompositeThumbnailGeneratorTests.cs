// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public class CompositeThumbnailGeneratorTests : AssetThumbnailGeneratorTests
{
    protected override string Name()
    {
        return "composite";
    }

    protected override IAssetThumbnailGenerator CreateSut()
    {
        var httpClientFactory =
            new ServiceCollection()
                .AddHttpClient()
                .BuildServiceProvider()
                .GetRequiredService<IHttpClientFactory>();

        return new CompositeThumbnailGenerator(
        [
            new ImageSharpThumbnailGenerator(httpClientFactory),
            new ImageMagickThumbnailGenerator()
        ]);
    }
}
