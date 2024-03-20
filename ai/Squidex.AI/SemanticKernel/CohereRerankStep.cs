// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Memory;

namespace Squidex.AI.SemanticKernel;

public sealed class CohereRerankStep : IRagPipelineStep
{
    private readonly CohereRerankOptions options;
    private readonly IHttpClientFactory httpClientFactory;

    private sealed class RerankResponse
    {
        [JsonPropertyName("results")]
        required public RerankResult[] Results { get; set; }
    }

    private sealed class RerankResult
    {
        [JsonPropertyName("key")]
        public RerankDocument? Document { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("relevance_score")]
        public float RelevanceScore { get; set; }
    }

    private sealed class RerankDocument
    {
        [JsonPropertyName("id")]
        required public string Id { get; set; }

        [JsonPropertyName("text")]
        required public string Text { get; set; }
    }

    public CohereRerankStep(IHttpClientFactory httpClientFactory, IOptionsFactory<CohereRerankOptions> optionsFactory, string name)
    {
        this.httpClientFactory = httpClientFactory;

        options = optionsFactory.Create(name);
    }

    public async IAsyncEnumerable<MemoryQueryResult> ProcessAsync(RagPipelineContext context, IAsyncEnumerable<MemoryQueryResult> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sourceTexts = new List<object>();
        var sourceResults = new List<MemoryQueryResult>();

        await foreach (var result in source.WithCancellation(cancellationToken))
        {
            var metadata = (JsonObject)JsonNode.Parse(result.Metadata.AdditionalMetadata)!;

            if (metadata.TryGetPropertyValue("text", out var text) && text?.GetValueKind() == JsonValueKind.String)
            {
                sourceTexts.Add(new { text = text.ToJsonString(), id = result.Metadata.Id });
            }

            sourceResults.Add(result);
        }

        if (sourceTexts.Count == 0 || sourceTexts.Count != sourceResults.Count)
        {
            foreach (var item in sourceResults)
            {
                yield return item;
            }

            yield break;
        }

        var body = new
        {
            context,
            documents = sourceTexts
        };

        using var httpClient = GetClient();
        using var httpResponse = await httpClient.PostAsJsonAsync("https://api.cohere.ai/v1/rerank", body, cancellationToken);

        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content.ReadFromJsonAsync<RerankResponse>(cancellationToken)
             ?? throw new InvalidOperationException("Failed to deserialize response");

        foreach (var item in response.Results)
        {
            var record = sourceResults[item.Index];

            yield return new MemoryQueryResult(record.Metadata, item.RelevanceScore, record.Embedding);
        }
    }

    private HttpClient GetClient()
    {
        var httpClient = httpClientFactory.CreateClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");

        return httpClient;
    }
}
