﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using OpenAI;
using OpenAI.ObjectModels;

namespace Squidex.Text.ChatBots.OpenAI;

public sealed class OpenAIChatBotOptions : OpenAiOptions
{
    public string Model { get; set; } = Models.Gpt_3_5_Turbo;

    public string[]? SystemMessages { get; set; }

    public int? MaxAnswerTokens { get; set; }

    public int MaxContextLength { get; set; } = 4000;

    public int CharactersPerToken { get; set; } = 5;

    public float? Temperature { get; set; }

    public decimal PricePerInputTokenInEUR { get; set; } = 0.003m / 1000;

    public decimal PricePerOutputTokenInEUR { get; set; } = 0.004m / 1000;
}
