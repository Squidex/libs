// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Betalgo.Ranul.OpenAI.Managers;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Microsoft.Extensions.Options;
using Squidex.Assets;

namespace Squidex.AI.Implementation.OpenAI;

public sealed class DallETool(
    IOptions<DallEOptions> options,
    IAssetStore assetStore,
    IAssetThumbnailGenerator assetThumbnailGenerator,
    IChatProvider chatProvider,
    IHttpImageEndpoint httpImageEndpoint,
    IHttpClientFactory httpClientFactory)
    : IImageTool
{
    private const string DataPrefix = "dall_e_image";
    private readonly OpenAIService service = new OpenAIService(options.Value);
    private readonly DallEOptions options = options.Value;

    public ToolSpec Spec { get; } =
        new ToolSpec("dall-e", "Dall-E", "Generates images based on queries.")
        {
            Arguments =
            {
                ["query"] = new ToolStringArgumentSpec("The query.")
                {
                    IsRequired = true,
                },
            },
        };

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

    public ToolContext CreateRequest(ImageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var toolContext = new ToolContext
        {
            Arguments = new Dictionary<string, ToolValue>
            {
                ["query"] = new ToolStringValue(request.Query),
            },
            ChatAgent = request.ChatAgent,
            Context = request.Context,
            ToolTag = true,
            ToolData = request.ToolData,
        };

        return toolContext;
    }

    public async Task<string> ExecuteAsync(ToolContext toolContext,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(toolContext);

        if (!toolContext.Arguments.TryGetValue("query", out var queryArg))
        {
            throw new ChatException("Missing argument 'query'.");
        }

        var request = new ImageCreateRequest(queryArg.ToString())
        {
            Model = options.Model,
            Size = options.Size,
            Style = options.Style,
            Quality = options.Quality,
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

        var format =
            toolContext.ToolTag is true ?
            options.PlainResult :
            options.DefaultResult;

        var result = format.Replace("{url}", url, StringComparison.Ordinal);

        if (format.Contains("{name}", StringComparison.Ordinal))
        {
            var fileName = await GenerateFileNameAsync(request.Prompt, toolContext, ct);

            result = result.Replace("{name}", fileName, StringComparison.Ordinal);
        }

        return result;
    }

    private async Task<string> GenerateFileNameAsync(string query, ToolContext toolContext,
        CancellationToken ct)
    {
        var result = await chatProvider.PromptAsync(
            options.ImageNamePattern.Replace("{query}", query, StringComparison.Ordinal),
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

        await using var imageSource = await httpResponse.Content.ReadAsStreamAsync(ct);
        await using var imageFile = TempHelper.GetTempStream();

        var mimeType = httpResponse.Content.Headers.ContentType?.ToString();

        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ChatException("Request failed because the file does not contain a content type.");
        }

        var resizeOptions = new ResizeOptions
        {
            Format = ImageFormat.WEBP,
        };

        await assetThumbnailGenerator.CreateThumbnailAsync(imageSource, mimeType, imageFile, resizeOptions, ct);
        imageFile.Position = 0;

        await assetStore.UploadAsync(imagePath, imageFile, true, ct);
        imageFile.Position = 0;

        if (toolContext != null)
        {
            toolContext.ToolData[$"{DataPrefix}_{imageId}"] = imagePath;
        }

        return imageUrl;
    }
}
