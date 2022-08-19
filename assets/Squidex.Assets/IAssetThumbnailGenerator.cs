// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;

namespace Squidex.Assets
{
    public interface IAssetThumbnailGenerator
    {
        bool CanReadAndWrite(string mimeType);

        bool CanComputeBlurHash();

        bool IsResizable(string mimeType, ResizeOptions options, [MaybeNullWhen(false)] out string? destinationMimeType);

        Task<ImageInfo?> GetImageInfoAsync(Stream source, string mimeType,
            CancellationToken ct = default);

        Task<string?> ComputeBlurHashAsync(Stream source, string mimeType, BlurOptions options,
            CancellationToken ct = default);

        Task FixOrientationAsync(Stream source, string mimeType, Stream destination,
            CancellationToken ct = default);

        Task CreateThumbnailAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
            CancellationToken ct = default);
    }
}
