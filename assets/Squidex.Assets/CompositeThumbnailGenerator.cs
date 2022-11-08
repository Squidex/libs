// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public sealed class CompositeThumbnailGenerator : AssetThumbnailGeneratorBase
{
    private readonly SemaphoreSlim maxTasks;
    private readonly IEnumerable<IAssetThumbnailGenerator> inners;

    public CompositeThumbnailGenerator(IEnumerable<IAssetThumbnailGenerator> inners, int maxTasks = 0)
    {
        if (maxTasks <= 0)
        {
            maxTasks = Math.Max(Environment.ProcessorCount / 4, 1);
        }

        this.maxTasks = new SemaphoreSlim(maxTasks);

        this.inners = inners;
    }

    public override bool CanReadAndWrite(string mimeType)
    {
        return mimeType != null && inners.Any(x => x.CanReadAndWrite(mimeType));
    }

    public override bool CanComputeBlurHash()
    {
        return inners.Any(x => x.CanComputeBlurHash());
    }

    protected override async Task CreateThumbnailCoreAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
        CancellationToken ct = default)
    {
        await maxTasks.WaitAsync(ct);
        try
        {
            var targetMimeType = options.Format?.ToMimeType() ?? mimeType;

            foreach (var inner in inners)
            {
                if (inner.CanReadAndWrite(mimeType) && inner.CanReadAndWrite(targetMimeType))
                {
                    await inner.CreateThumbnailAsync(source, mimeType, destination, options, ct);
                    return;
                }
            }
        }
        finally
        {
            maxTasks.Release();
        }

        await source.CopyToAsync(destination, ct);
    }

    protected override async Task FixOrientationCoreAsync(Stream source, string mimeType, Stream destination,
        CancellationToken ct = default)
    {
        await maxTasks.WaitAsync(ct);
        try
        {
            foreach (var inner in inners)
            {
                if (inner.CanReadAndWrite(mimeType))
                {
                    await inner.FixOrientationAsync(source, mimeType, destination, ct);
                    return;
                }
            }
        }
        finally
        {
            maxTasks.Release();
        }

        throw new InvalidOperationException("No thumbnail generator registered.");
    }

    protected override async Task<string?> ComputeBlurHashCoreAsync(Stream source, string mimeType, BlurOptions options,
        CancellationToken ct = default)
    {
        foreach (var inner in inners.Where(x => x.CanReadAndWrite(mimeType) && x.CanComputeBlurHash()))
        {
            var result = await inner.ComputeBlurHashAsync(source, mimeType, options, ct);

            if (result != null)
            {
                return result;
            }

            source.Position = 0;
        }

        return null;
    }

    protected override async Task<ImageInfo?> GetImageInfoCoreAsync(Stream source, string mimeType,
        CancellationToken ct = default)
    {
        foreach (var inner in inners)
        {
            var result = await inner.GetImageInfoAsync(source, mimeType, ct);

            if (result != null)
            {
                return result;
            }

            source.Position = 0;
        }

        return null;
    }
}
