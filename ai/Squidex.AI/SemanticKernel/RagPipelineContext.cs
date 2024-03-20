// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.SemanticKernel;

namespace Squidex.AI.SemanticKernel;

public sealed class RagPipelineContext
{
    required public string Query { get; set; }

    public int Limit { get; set; } = 10;

    public float MinRelevanceScore { get; set; }

    public ReadOnlyMemory<float> Embedding { get; set; }

    public Kernel? Kernel { get; set; }
}