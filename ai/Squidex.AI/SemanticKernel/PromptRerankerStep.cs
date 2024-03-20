// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Memory;

namespace Squidex.AI.SemanticKernel;

internal sealed class PromptRerankerStep : IRagPipelineStep
{
    private readonly PromptRerankerOptions options;

    public PromptRerankerStep(IOptionsFactory<PromptRerankerOptions> optionsFactory, string name)
    {
        options = optionsFactory.Create(name);
    }

    public async IAsyncEnumerable<MemoryQueryResult> ProcessAsync(RagPipelineContext context, IAsyncEnumerable<MemoryQueryResult> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var completionService = context.Kernel?.GetRequiredService<IChatCompletionService>();

        if (completionService == null)
        {
            await foreach (var item in source.WithCancellation(cancellationToken))
            {
                yield return item;
            }

            yield break;
        }

        var sourceTexts = new List<(string Text, string Id, int Rank, int Index)>();
        var sourceResults = new List<MemoryQueryResult>();

        await foreach (var result in source.WithCancellation(cancellationToken))
        {
            var metadata = (JsonObject)JsonNode.Parse(result.Metadata.AdditionalMetadata)!;

            if (metadata.TryGetPropertyValue("text", out var text) && text?.GetValueKind() == JsonValueKind.String)
            {
                sourceTexts.Add((text.ToJsonString(), result.Metadata.Id, 0, sourceTexts.Count));
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

        var query = context.Query;

        await Parallel.ForEachAsync(sourceTexts, cancellationToken, async (doc, ct) =>
        {
            var result = await completionService.GetChatMessageContentAsync(options.GetPrompt(query, doc.Text), null, context.Kernel, ct);

            var item = result.Items.FirstOrDefault();

            if (int.TryParse(result.Content, CultureInfo.InvariantCulture, out var ranking))
            {
                sourceTexts[doc.Index] = (doc.Text, doc.Id, ranking, doc.Index);
            }
        });

        foreach (var doc in sourceTexts)
        {
            var original = sourceResults.Find(x => x.Metadata.Id == doc.Id)!;

            yield return original;
        }
    }
}
