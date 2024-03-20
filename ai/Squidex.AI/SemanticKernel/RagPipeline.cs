// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Memory;

namespace Squidex.AI.SemanticKernel;

public sealed class RagPipeline : IRagPipeline
{
    private readonly List<IRagPipelineStep> steps = [];

    public RagPipeline(IOptionsFactory<RagPipelineOptions> optionsFactory, string name, IServiceProvider serviceProvider)
    {
        var options = optionsFactory.Create(name);

        foreach (var factory in options.StepFactories)
        {
            steps.Add(factory(serviceProvider));
        }

        steps.Reverse();
    }

    public IAsyncEnumerable<MemoryQueryResult> SearchAsync(RagPipelineContext context,
        CancellationToken cancellationToken = default)
    {
        var source = AsyncEnumerable.Empty<MemoryQueryResult>();

        foreach (var step in steps)
        {
            source = step.ProcessAsync(context, source, cancellationToken);
        }

        return source;
    }
}
