// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Squidex.AI.SemanticKernel;

namespace Microsoft.Extensions.DependencyInjection;

public static class RagServiceCollectionExtensions
{
    public static RagPipelineBuilder AddRagPipeline(this IKernelBuilder builder, string name)
    {
        builder.Services.AddKeyedSingleton<IRagPipeline>(name, (c, n) => ActivatorUtilities.CreateInstance<RagPipeline>(c, n!.ToString()!));

        return new RagPipelineBuilder(builder.Services, name);
    }

    public static RagPipelineBuilder AddCalculateEmbeddings(this RagPipelineBuilder builder, ITextEmbeddingGenerationService embeddingGenerationService)
    {
        return builder.AddStep(c =>
            ActivatorUtilities.CreateInstance<CalculateEmbeddingsStep>(c, embeddingGenerationService));
    }

    public static RagPipelineBuilder AddCalculateEmbeddings(this RagPipelineBuilder builder, object? serviceId = null)
    {
        return builder.AddStep(c =>
            ActivatorUtilities.CreateInstance<CalculateEmbeddingsStep>(c, c.GetRequiredKeyedService<ITextEmbeddingGenerationService>(serviceId)));
    }

    public static RagPipelineBuilder AddCalculateEmbeddings(this RagPipelineBuilder builder, Func<IServiceProvider, ITextEmbeddingGenerationService> factory)
    {
        return builder.AddStep(c =>
            ActivatorUtilities.CreateInstance<CalculateEmbeddingsStep>(c, factory(c)));
    }

    public static RagPipelineBuilder AddSearchInMemoryStore(this RagPipelineBuilder builder, IMemoryStore memoryStore, string collectionName, Action<MemoryStoreStepOptions>? configure = null)
    {
        builder.Configure(collectionName, configure);

        return builder.AddStep(c =>
            ActivatorUtilities.CreateInstance<MemoryStoreStep>(c, builder.Name, memoryStore));
    }

    public static RagPipelineBuilder AddSearchInMemoryStore(this RagPipelineBuilder builder, string collectionName, object? serviceId = null, Action<MemoryStoreStepOptions>? configure = null)
    {
        builder.Configure(collectionName, configure);

        return builder.AddStep(c =>
            ActivatorUtilities.CreateInstance<MemoryStoreStep>(c, builder.Name, c.GetRequiredKeyedService<IMemoryStore>(serviceId)));
    }

    public static RagPipelineBuilder AddSearchInMemoryStore(this RagPipelineBuilder builder, Func<IServiceProvider, IMemoryStore> factory, string collectionName, Action<MemoryStoreStepOptions>? configure = null)
    {
        builder.Configure(collectionName, configure);

        return builder.AddStep(c =>
            ActivatorUtilities.CreateInstance<MemoryStoreStep>(c, builder.Name, factory(c)));
    }

    public static RagPipelineBuilder AddCohereRerank(this RagPipelineBuilder builder, string apiKey, Action<CohereRerankOptions>? configure = null)
    {
        builder.Configure(apiKey, configure);

        return builder.AddStep(c =>
            ActivatorUtilities.CreateInstance<CohereRerankStep>(c, builder.Name, apiKey));
    }

    public static RagPipelineBuilder AddPromptReranker(this RagPipelineBuilder builder, Action<PromptRerankerOptions>? configure = null)
    {
        builder.Configure(configure);

        return builder.AddStep(c =>
            ActivatorUtilities.CreateInstance<PromptRerankerStep>(c, builder.Name));
    }

    private static void Configure(this RagPipelineBuilder builder, string collectionName, Action<MemoryStoreStepOptions>? configure)
    {
        builder.Services.Configure<MemoryStoreStepOptions>(builder.Name, options =>
        {
            options.CollectionName = collectionName;
            configure?.Invoke(options);
        });
    }

    private static void Configure(this RagPipelineBuilder builder, string apiKey, Action<CohereRerankOptions>? configure)
    {
        builder.Services.Configure<CohereRerankOptions>(builder.Name, options =>
        {
            options.ApiKey = apiKey;
            configure?.Invoke(options);
        });
    }

    private static void Configure(this RagPipelineBuilder builder, Action<PromptRerankerOptions>? configure)
    {
        builder.Services.Configure<PromptRerankerOptions>(builder.Name, options =>
        {
            configure?.Invoke(options);
        });
    }
}
