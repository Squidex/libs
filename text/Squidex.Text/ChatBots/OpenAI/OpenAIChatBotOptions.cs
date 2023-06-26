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

    public int? MaxTokens { get; set; }
}
