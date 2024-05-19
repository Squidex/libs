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

public sealed class DallETool : IChatTool
{
    private readonly OpenAIService service;
    private readonly OpenAIOptions options;
    private readonly IAssetStore assetStore;
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
        IOptions<OpenAIOptions> options,
        IAssetStore assetStore,
        IHttpImageEndpoint httpImageEndpoint,
        IHttpClientFactory httpClientFactory)
    {
        service = new OpenAIService(options.Value);

        this.options = options.Value;
        this.assetStore = assetStore;
        this.httpImageEndpoint = httpImageEndpoint;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<string> ExecuteAsync(IChatAgent agent, ChatContext context, Dictionary<string, ToolValue> arguments,
        CancellationToken ct)
    {
        if (!arguments.TryGetValue("query", out var queryArg))
        {
            throw new ChatException("Missing argument 'query'.");
        }

        var query = queryArg.ToString();

        var request = new ImageCreateRequest
        {
            Prompt = query
        };

        var response = await service.Image.CreateImage(request, ct);

        if (response.Error != null)
        {
            throw new ChatException($"Request failed with internal error: {response.Error.Message}. HTTP {response.HttpStatusCode}");
        }

        if (!response.Successful)
        {
            throw new ChatException($"Request failed with unknown error. HTTP {response.HttpStatusCode}");
        }

        var url = response.Results[0].Url;

        if (!options.DownloadImage)
        {
            return url;
        }

        var imageId = Guid.NewGuid().ToString();

        var imagePath = options.ImagePathPattern.Replace("{IMAGE_ID}", imageId, StringComparison.Ordinal);
        var imageUrl = httpImageEndpoint.GetUrl(imagePath);

        using var httpClient = httpClientFactory.CreateClient(url);

        await using (var httpResponse = await httpClient.GetStreamAsync(url, ct))
        {
            await assetStore.UploadAsync(imagePath, httpResponse, true, ct);
        }

        return imageUrl;
    }
}
