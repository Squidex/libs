// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Betalgo.Ranul.OpenAI.Managers;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Microsoft.Extensions.Options;

namespace Squidex.AI.Implementation.OpenAI;

public sealed class OpenAIEmbeddings(IOptions<OpenAIEmbeddingsOptions> options) : IEmbeddings
{
    private readonly OpenAIEmbeddingsOptions options = options.Value;
    private readonly OpenAIService service = new OpenAIService(options.Value);

    public async Task<ReadOnlyMemory<double>> CalculateEmbeddingsAsync(string query,
        CancellationToken ct)
    {
        var request = new EmbeddingCreateRequest
        {
            Model = options.ModelName,
            Input = query,
            InputAsList = null,
        };

        var response = await service.Embeddings.CreateEmbedding(request, ct);

        if (response.Error != null)
        {
            throw new ChatException($"Request failed with internal error: {response.Error.Message}. HTTP {response.HttpStatusCode}");
        }

        if (!response.Successful)
        {
            throw new ChatException($"Request failed with unknown error. HTTP {response.HttpStatusCode}");
        }

        return response.Data[0].Embedding.ToArray().AsMemory();
    }
}
