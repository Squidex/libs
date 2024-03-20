// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.SemanticKernel.Memory;

namespace Squidex.AI.SemanticKernel;

public interface IRagPipeline
{
    IAsyncEnumerable<MemoryQueryResult> SearchAsync(string query,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(new RagPipelineContext { Query = query }, cancellationToken);
    }

    IAsyncEnumerable<MemoryQueryResult> SearchAsync(RagPipelineContext context,
        CancellationToken cancellationToken = default);
}
