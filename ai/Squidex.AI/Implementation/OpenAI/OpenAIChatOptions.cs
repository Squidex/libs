// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.ObjectModels;

namespace Squidex.AI.Implementation.OpenAI;

public sealed class OpenAIChatOptions : OpenAIOptions
{
    public string Model { get; set; } = Models.Gpt_4o;

    public int? MaxTokens { get; set; }

    public int MaxIterations { get; set; } = 2;

    public int CharactersPerToken { get; set; } = 5;

    public int? Seed { get; set; }

    public float? Temperature { get; set; }
}
