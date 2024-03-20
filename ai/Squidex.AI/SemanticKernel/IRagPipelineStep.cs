// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.SemanticKernel.Memory;

#pragma warning disable // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Squidex.AI.SemanticKernel;

public interface IRagPipelineStep
{
    IAsyncEnumerable<MemoryQueryResult> ProcessAsync(RagPipelineContext context, IAsyncEnumerable<MemoryQueryResult> source,
        CancellationToken cancellationToken);
}