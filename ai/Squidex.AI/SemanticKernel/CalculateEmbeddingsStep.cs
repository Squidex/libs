// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;

namespace Squidex.AI.SemanticKernel;

internal sealed class CalculateEmbeddingsStep : IRagPipelineStep
{
    private readonly ITextEmbeddingGenerationService textEmbeddingGenerationService;

    public CalculateEmbeddingsStep(ITextEmbeddingGenerationService textEmbeddingGenerationService)
    {
        this.textEmbeddingGenerationService = textEmbeddingGenerationService;
    }

    public async IAsyncEnumerable<MemoryQueryResult> ProcessAsync(RagPipelineContext context, IAsyncEnumerable<MemoryQueryResult> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        context.Embedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(context.Query, context.Kernel, cancellationToken);

        await foreach (var result in source.WithCancellation(cancellationToken))
        {
            yield return result;
        }
    }
}
