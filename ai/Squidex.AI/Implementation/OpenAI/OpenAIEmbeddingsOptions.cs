// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using OpenAI;
using OpenAI.ObjectModels;

namespace Squidex.AI.Implementation.OpenAI;

public sealed class OpenAIEmbeddingsOptions : OpenAiOptions
{
    public string ModelName { get; set; } = Models.TextEmbeddingV3Large;
}
