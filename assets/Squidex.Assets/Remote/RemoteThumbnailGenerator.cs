// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

namespace Squidex.Assets.Remote;

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
        using var httpClient = httpClientFactory.CreateClient("Resize");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/blur?{BuildQueryString(options)}")
        {
            Content = new StreamContent(source)
        };

        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

        using var httpResponse = await httpClient.SendAsync(httpRequest, ct);

        httpResponse.EnsureSuccessStatusCode();

        var result = await httpResponse.Content.ReadAsStringAsync(ct);

        if (string.IsNullOrWhiteSpace(result))
        {
            result = null;
        }

        return result;
    }

    protected override async Task CreateThumbnailCoreAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
        CancellationToken ct = default)
    {
        using var httpClient = httpClientFactory.CreateClient("Resize");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/resize{BuildQueryString(options)}")
        {
            Content = new StreamContent(source)
        };

        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

        using var httpResonse = await httpClient.SendAsync(httpRequest, ct);

        httpResonse.EnsureSuccessStatusCode();

        await httpResonse.Content.CopyToAsync(destination, ct);
    }

    protected override async Task FixCoreAsync(Stream source, string mimeType, Stream destination,
        CancellationToken ct = default)
    {
        using var httpClient = httpClientFactory.CreateClient("Resize");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/orient")
        {
            Content = new StreamContent(source)
        };

        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

        var httpResponse = await httpClient.SendAsync(httpRequest, ct);

        httpResponse.EnsureSuccessStatusCode();

        await httpResponse.Content.CopyToAsync(destination, ct);
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
