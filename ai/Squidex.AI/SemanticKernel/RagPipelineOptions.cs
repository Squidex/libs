// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.SemanticKernel;

public sealed class RagPipelineOptions
{
    public List<Func<IServiceProvider, IRagPipelineStep>> StepFactories { get; } = [];
}
