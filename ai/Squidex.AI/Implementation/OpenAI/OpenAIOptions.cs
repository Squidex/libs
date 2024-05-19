// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using OpenAI;
using OpenAI.ObjectModels;

namespace Squidex.AI.Implementation.OpenAI;

public sealed class OpenAIOptions : OpenAiOptions
{
    public string Model { get; set; } = Models.Gpt_3_5_Turbo;

    public int? MaxTokens { get; set; }

    public int CharactersPerToken { get; set; } = 5;

    public int? Seed { get; set; }

    public float? Temperature { get; set; }

    public string ImagePathPattern { get; set; } = "dall-e/{IMAGE_ID}";

    public bool DownloadImage { get; set; } = false;
}
