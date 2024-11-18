// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.ObjectModels;

namespace Squidex.AI.Implementation.OpenAI;

public sealed class OpenAIEmbeddingsOptions : OpenAIOptions
{
    public string ModelName { get; set; } = Models.TextEmbeddingV3Large;
}
