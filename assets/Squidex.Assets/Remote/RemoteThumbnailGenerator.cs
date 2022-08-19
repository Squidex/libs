// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using Squidex.Assets.Internal;

namespace Squidex.Assets.Remote
{
    public sealed class RemoteThumbnailGenerator : AssetThumbnailGeneratorBase
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IAssetThumbnailGenerator inner;

        public RemoteThumbnailGenerator(IHttpClientFactory httpClientFactory, IAssetThumbnailGenerator inner)
        {
            this.httpClientFactory = httpClientFactory;

            this.inner = inner;
        }

        public override bool CanReadAndWrite(string mimeType)
        {
            return inner.CanReadAndWrite(mimeType);
        }

        public override bool CanComputeBlurHash()
        {
            return inner.CanComputeBlurHash();
        }

        public override bool IsResizable(string mimeType, ResizeOptions options, [MaybeNullWhen(false)] out string? destinationMimeType)
        {
            return inner.IsResizable(mimeType, options, out destinationMimeType);
        }

        protected override async Task<string?> ComputeBlurHashCoreAsync(Stream source, string mimeType, BlurOptions options,
            CancellationToken ct = default)
        {
            using (var httpClient = httpClientFactory.CreateClient("Resize"))
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/blur?{BuildQueryString(options)}")
                {
                    Content = new StreamContent(source)
                };

                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

                var response = await httpClient.SendAsync(requestMessage, ct);

                response.EnsureSuccessStatusCode();
#if NET6_0
                var result = await response.Content.ReadAsStringAsync(ct);
#else
                var result = await response.Content.ReadAsStringAsync();
#endif
                if (string.IsNullOrWhiteSpace(result))
                {
                    result = null;
                }

                return null;
            }
        }

        protected override async Task CreateThumbnailCoreAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
            CancellationToken ct = default)
        {
            using (var httpClient = httpClientFactory.CreateClient("Resize"))
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/resize{BuildQueryString(options)}")
                {
                    Content = new StreamContent(source)
                };

                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

                var response = await httpClient.SendAsync(requestMessage, ct);

                response.EnsureSuccessStatusCode();
#if NET6_0
                await response.Content.CopyToAsync(destination, ct);
#else
                await response.Content.CopyToAsync(destination);
#endif
            }
        }

        protected override async Task FixOrientationCoreAsync(Stream source, string mimeType, Stream destination,
            CancellationToken ct = default)
        {
            using (var httpClient = httpClientFactory.CreateClient("Resize"))
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/orient")
                {
                    Content = new StreamContent(source)
                };

                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

                var response = await httpClient.SendAsync(requestMessage, ct);

                response.EnsureSuccessStatusCode();
#if NET6_0
                await response.Content.CopyToAsync(destination, ct);
#else
                await response.Content.CopyToAsync(destination);
#endif
            }
        }

        protected override Task<ImageInfo?> GetImageInfoCoreAsync(Stream source, string mimeType,
            CancellationToken ct = default)
        {
            return inner.GetImageInfoAsync(source, mimeType, ct);
        }

        private static string BuildQueryString(IOptions options)
        {
            var sb = new StringBuilder();

            foreach (var (key, value) in options.ToParameters())
            {
                if (sb.Length > 0)
                {
                    sb.Append('&');
                }
                else
                {
                    sb.Append('?');
                }

                sb.Append(key);
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(value));
            }

            return sb.ToString();
        }
    }
}
