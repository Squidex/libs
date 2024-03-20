// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Memory;

namespace Squidex.AI.SemanticKernel;

public sealed class MemoryStoreStep : IRagPipelineStep
{
    private readonly IMemoryStore memoryStore;
    private readonly MemoryStoreStepOptions options;

    public MemoryStoreStep(IOptionsFactory<MemoryStoreStepOptions> optionsFactory, string name, IMemoryStore memoryStore)
    {
        options = optionsFactory.Create(name);

        this.memoryStore = memoryStore;
    }

    public async IAsyncEnumerable<MemoryQueryResult> ProcessAsync(RagPipelineContext context, IAsyncEnumerable<MemoryQueryResult> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (context.Embedding.Length == 0)
        {
            throw new InvalidOperationException("Embedding has not been calculated yet.");
        }

        var records = memoryStore.GetNearestMatchesAsync(options.CollectionName,
            context.Embedding,
            context.Limit,
            context.MinRelevanceScore,
            options.WithEmbeddings,
            cancellationToken);

        await foreach (var (record, relevance) in records.WithCancellation(cancellationToken))
        {
            yield return MemoryQueryResult.FromMemoryRecord(record, relevance);
        }
    }
}
