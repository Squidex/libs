// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public class ImageMagickThumbnailGeneratorTests : AssetThumbnailGeneratorTests
{
    protected override bool SupportsBlurHash => false;

    protected override string Name()
    {
        return "magick";
    }

    protected override IAssetThumbnailGenerator CreateSut()
    {
        return new ImageMagickThumbnailGenerator();
    }
}
