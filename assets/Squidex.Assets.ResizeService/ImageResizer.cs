// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Log;

namespace Squidex.Assets.ResizeService
{
    public sealed class ImageResizer
    {
        private readonly IAssetThumbnailGenerator assetThumbnailGenerator;

        public ImageResizer(IAssetThumbnailGenerator assetThumbnailGenerator)
        {
            this.assetThumbnailGenerator = assetThumbnailGenerator;
        }

        public void Map(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/blur", async context =>
            {
                await BlurAsync(context);
            });

            endpoints.MapPost("/orient", async context =>
            {
                await OrientAsync(context);
            });

            endpoints.MapPost("/resize", async context =>
            {
                await ResizeAsync(context);
            });
        }

        private async Task BlurAsync(HttpContext context)
        {
            await using var tempStream = GetTempStream();

            await ReadToTempStreamAsync(context, tempStream);

            try
            {
                var options = BlurOptions.Parse(context.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()));

                var hash = await assetThumbnailGenerator.ComputeBlurHashAsync(
                    tempStream,
                    context.Request.ContentType ?? "image/png",
                    options,
                    context.RequestAborted);

                if (hash != null)
                {
                    await context.Response.WriteAsync(hash, context.RequestAborted);
                }
            }
            catch (Exception ex)
            {
                var log = context.RequestServices.GetRequiredService<ILogger<ImageResizer>>();

                log.LogError(ex, "Failed to orient image.");

                context.Response.StatusCode = 400;
            }
        }

        private async Task OrientAsync(HttpContext context)
        {
            await using var tempStream = GetTempStream();

            await ReadToTempStreamAsync(context, tempStream);

            try
            {
                await assetThumbnailGenerator.FixOrientationAsync(
                    tempStream,
                    context.Request.ContentType ?? "image/png",
                    context.Response.Body,
                    context.RequestAborted);
            }
            catch (Exception ex)
            {
                var log = context.RequestServices.GetRequiredService<ILogger<ImageResizer>>();

                log.LogError(ex, "Failed to orient image.");

                context.Response.StatusCode = 400;
            }
        }

        private async Task ResizeAsync(HttpContext context)
        {
            await using var tempStream = GetTempStream();

            await ReadToTempStreamAsync(context, tempStream);

            try
            {
                var options = ResizeOptions.Parse(context.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()));

                await assetThumbnailGenerator.CreateThumbnailAsync(
                    tempStream,
                    context.Request.ContentType ?? "image/png",
                    context.Response.Body, options,
                    context.RequestAborted);
            }
            catch (Exception ex)
            {
                var log = context.RequestServices.GetRequiredService<ILogger<ImageResizer>>();

                log.LogError(ex, "Failed to resize image.");

                context.Response.StatusCode = 400;
            }
        }

        private static async Task ReadToTempStreamAsync(HttpContext context, Stream tempStream)
        {
            await context.Request.Body.CopyToAsync(tempStream, context.RequestAborted);

            tempStream.Position = 0;
        }

        private static Stream GetTempStream()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            var stream = new FileStream(tempPath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None, 4096,
                FileOptions.DeleteOnClose);

            return stream;
        }
    }
}
