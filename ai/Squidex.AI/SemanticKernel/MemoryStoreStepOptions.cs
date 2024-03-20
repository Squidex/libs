// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.SemanticKernel;

public sealed class MemoryStoreStepOptions
{
    public string CollectionName { get; set; }

    public bool WithEmbeddings { get; set; }
}
