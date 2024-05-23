// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using OpenAI;

namespace Squidex.AI.Implementation.OpenAI;

public sealed class DallEOptions : OpenAiOptions
{
    public string? Model { get; set; } = "dall-e-3";

    public string? Style { get; set; }

    public string? Size { get; set; }

    public string? Quality { get; set; }

    public string ImagePathPattern { get; set; } = "dall-e/{IMAGE_ID}";

    public bool DownloadImage { get; set; } = false;
}
