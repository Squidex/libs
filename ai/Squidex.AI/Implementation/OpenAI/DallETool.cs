// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using Squidex.Assets;

namespace Squidex.AI.Implementation.OpenAI;

public sealed class DallETool : IImageTool
{
    private const string DataPrefix = "dall_e_image";
    private readonly OpenAIService service;
    private readonly DallEOptions options;
    private readonly IAssetStore assetStore;
    private readonly IAssetThumbnailGenerator assetThumbnailGenerator;
    private readonly IChatProvider chatProvider;
    private readonly IHttpImageEndpoint httpImageEndpoint;
    private readonly IHttpClientFactory httpClientFactory;

    public ToolSpec Spec { get; } =
        new ToolSpec("dall-e", "Dall-E", "Generates images based on queries.")
        {
            Arguments =
            {
                ["query"] = new ToolStringArgumentSpec("The query.")
                {
                    IsRequired = true
                }
            }
        };

    public DallETool(
        IOptions<DallEOptions> options,
        IAssetStore assetStore,
        IAssetThumbnailGenerator assetThumbnailGenerator,
        IChatProvider chatProvider,
        IHttpImageEndpoint httpImageEndpoint,
        IHttpClientFactory httpClientFactory)
    {
        service = new OpenAIService(options.Value);

        this.options = options.Value;
        this.assetStore = assetStore;
        this.assetThumbnailGenerator = assetThumbnailGenerator;
        this.chatProvider = chatProvider;
        this.httpImageEndpoint = httpImageEndpoint;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task CleanupAsync(Dictionary<string, string> toolData,
        CancellationToken ct)
    {
        foreach (var (key, value) in toolData)
        {
            if (key.StartsWith(DataPrefix, StringComparison.Ordinal))
            {
                await assetStore.DeleteAsync(value, ct);
            }
        }
    }

    public Task<string> GenerateAsync(ToolContext toolContext,
        CancellationToken ct)
    {
        return ExecuteCoreAsync(toolContext, options.GenerateResult, ct);
    }

    public Task<string> ExecuteAsync(ToolContext toolContext,
        CancellationToken ct)
    {
        return ExecuteCoreAsync(toolContext, options.PromptResult, ct);
    }

    private async Task<string> ExecuteCoreAsync(ToolContext toolContext, string format,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(toolContext);

        if (!toolContext.Arguments.TryGetValue("query", out var queryArg))
        {
            throw new ChatException("Missing argument 'query'.");
        }

        var query = queryArg.ToString();

        var request = new ImageCreateRequest
        {
            Model = options.Model,
            Prompt = query,
            Quality = options.Quality,
            Size = options.Size,
            Style = options.Style
        };

        var response = await service.Image.CreateImage(request, ct);

        if (response.Error != null)
        {
            throw new ChatException($"Request failed with internal error: {response.Error.Message}. HTTP {response.HttpStatusCode}.");
        }

        if (!response.Successful)
        {
            throw new ChatException($"Request failed with unknown error. HTTP {response.HttpStatusCode}.");
        }

        var url = response.Results[0].Url;

        if (options.DownloadImage)
        {
            url = await DownloadImageAsync(url, null, ct);
        }

        var result = format.Replace("{url}", url, StringComparison.Ordinal);

        if (format.Contains("{name}", StringComparison.Ordinal))
        {
            var fileName = await GenerateFileNameAsync(query, toolContext, ct);

            result = result.Replace("{name}", fileName, StringComparison.Ordinal);
        }

        return result;
    }

    private async Task<string> GenerateFileNameAsync(string query, ToolContext toolContext,
        CancellationToken ct)
    {
        var result = await chatProvider.PromptAsync(
            options.PrompFileName.Replace("{query}", query),
            toolContext,
            ct);

        if (result.Any(char.IsLower) && Uri.IsWellFormedUriString(result, UriKind.Relative))
        {
            return result;
        }

        return "image";
    }

    private async Task<string> DownloadImageAsync(string url, ToolContext? toolContext,
        CancellationToken ct)
    {
        var imageId = Guid.NewGuid().ToString();

        var imagePath = options.ImagePathPattern.Replace("{IMAGE_ID}", imageId, StringComparison.Ordinal);
        var imageUrl = httpImageEndpoint.GetUrl(imagePath);

        using var httpClient = httpClientFactory.CreateClient(url);
        using var httpResponse = await httpClient.GetAsync(url, ct);
        using var httpStream = await httpResponse.Content.ReadAsStreamAsync(ct);
        using var tempStream = TempHelper.GetTempStream();

        var mimeType = httpResponse.Content.Headers.ContentType?.ToString();

        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ChatException("Request failed because the file does not contain a content type.");
        }

        var resizeOptions = new ResizeOptions
        {
            Format = ImageFormat.WEBP
        };

        await assetThumbnailGenerator.CreateThumbnailAsync(httpStream, mimeType, tempStream, resizeOptions, ct);
        tempStream.Position = 0;

        await assetStore.UploadAsync(imagePath, tempStream, true, ct);
        tempStream.Position = 0;

        if (toolContext != null)
        {
            toolContext.ToolData[$"{DataPrefix}_{imageId}"] = imagePath;
        }

        return imageUrl;
    }
}
