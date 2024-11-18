// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;

namespace Squidex.Assets;

public abstract class AssetThumbnailGeneratorBase : IAssetThumbnailGenerator
{
    public virtual bool CanReadAndWrite(string mimeType)
    {
        return true;
    }

    public virtual bool CanComputeBlurHash()
    {
        return true;
    }

    public virtual bool IsResizable(string mimeType, ResizeOptions options, [MaybeNullWhen(false)] out string? destinationMimeType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentNullException.ThrowIfNull(options);

        destinationMimeType = null;

        // If we cannot read or write from the mime type we can just stop here.
        if (!CanReadAndWrite(mimeType))
        {
            return false;
        }

        string? targetMimeType;
        try
        {
            targetMimeType = options.Format?.ToMimeType();

            if (targetMimeType != null && (!CanReadAndWrite(targetMimeType) || targetMimeType == mimeType))
            {
                targetMimeType = null;
            }
        }
        catch (ArgumentException)
        {
            targetMimeType = null;
        }

        if (options.TargetWidth > 0 ||
            options.TargetHeight > 0 ||
            options.Quality > 0 ||
            targetMimeType != null ||
            Uri.IsWellFormedUriString(options.WatermarkUrl, UriKind.Absolute))
        {
            destinationMimeType = targetMimeType ?? mimeType;

            return true;
        }

        return false;
    }

    public async Task<ImageInfo?> GetImageInfoAsync(Stream source, string mimeType,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        // If we cannot read or write from the mime type we can just stop here.
        if (!CanReadAndWrite(mimeType))
        {
            return null;
        }

        return await GetImageInfoCoreAsync(source, mimeType, ct);
    }

    protected abstract Task<ImageInfo?> GetImageInfoCoreAsync(Stream source, string mimeType,
        CancellationToken ct = default);

    public async Task<string?> ComputeBlurHashAsync(Stream source, string mimeType, BlurOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentNullException.ThrowIfNull(options);

        // If we cannot read or write from the mime type we can just stop here.
        if (!CanReadAndWrite(mimeType))
        {
            return null;
        }

        return await ComputeBlurHashCoreAsync(source, mimeType, options, ct);
    }

    protected abstract Task<string?> ComputeBlurHashCoreAsync(Stream source, string mimeType, BlurOptions options,
        CancellationToken ct = default);

    public async Task FixAsync(Stream source, string mimeType, Stream destination,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentNullException.ThrowIfNull(destination);

        // If we cannot read or write from the mime type we can just stop here.
        if (!CanReadAndWrite(mimeType))
        {
            await source.CopyToAsync(destination, ct);
            return;
        }

        await FixCoreAsync(source, mimeType, destination, ct);
    }

    protected abstract Task FixCoreAsync(Stream source, string mimeType, Stream destination,
        CancellationToken ct = default);

    public async Task CreateThumbnailAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(options);

        if (!IsResizable(mimeType, options, out _))
        {
            await source.CopyToAsync(destination, ct);
            return;
        }

        await CreateThumbnailCoreAsync(source, mimeType, destination, options, ct);
    }

    protected abstract Task CreateThumbnailCoreAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
        CancellationToken ct = default);
}
