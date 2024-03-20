// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;

namespace Squidex.AI.SemanticKernel;

public sealed class RagPipelineBuilder(IServiceCollection services, string name)
{
    public IServiceCollection Services { get; } = services;

    public string Name { get; } = name;

    public RagPipelineBuilder AddStep(Func<IServiceProvider, IRagPipelineStep> factory)
    {
        Services.Configure<RagPipelineOptions>(Name, options =>
        {
            options.StepFactories.Add(factory);
        });

        return this;
    }
}
