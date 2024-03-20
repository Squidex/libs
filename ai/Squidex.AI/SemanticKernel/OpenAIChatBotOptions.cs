// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.SemanticKernel;

public sealed class OpenAIChatBotOptions
{
    public string[]? SystemMessages { get; set; }

    public int? MaxAnswerTokens { get; set; }

    public int MaxContextLength { get; set; } = 4000;

    public int CharactersPerToken { get; set; } = 5;

    public double? Temperature { get; set; }

    public decimal PricePerInputTokenInEUR { get; set; } = 0.003m / 1000;

    public decimal PricePerOutputTokenInEUR { get; set; } = 0.004m / 1000;

    public TimeSpan ConversationLifetime { get; set; } = TimeSpan.FromDays(3);
}
