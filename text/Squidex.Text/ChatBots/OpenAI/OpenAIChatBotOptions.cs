// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using OpenAI.ObjectModels;

namespace Squidex.Text.ChatBots.OpenAI;

public sealed class OpenAIChatBotOptions
{
    public string ApiKey { get; set; }

    public string Model { get; set; } = Models.ChatGpt3_5Turbo;

    public decimal PricePerInputTokenInEUR { get; set; } = 0.003m / 1000;

    public decimal PricePerOutputTokenInEUR { get; set; } = 0.004m / 1000;

    public int? MaxTokens { get; set; }
}
