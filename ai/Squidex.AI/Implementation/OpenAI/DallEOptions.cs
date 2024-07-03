// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using OpenAI;
using OpenAI.ObjectModels;

namespace Squidex.AI.Implementation.OpenAI;

public sealed class DallEOptions : OpenAiOptions
{
    public string? Model { get; set; } = Models.Dall_e_3;

    public string? Style { get; set; }

    public string? Size { get; set; }

    public string? Quality { get; set; }

    public string DefaultResult { get; set; } = "{ \"url\": \"{url}\" }";

    public string PlainResult { get; set; } = "![{name}]({url})";

    public string ImageNamePattern { get; set; } = "Generate a slugified file name for an image from the following query <QUERY>{query}</QUERY>.\\nDo not return other content.";

    public string ImagePathPattern { get; set; } = "dall-e/{IMAGE_ID}";

    public bool DownloadImage { get; set; } = false;
}
