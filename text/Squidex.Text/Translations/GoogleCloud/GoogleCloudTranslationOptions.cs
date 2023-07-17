// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.Translations.GoogleCloud;

public sealed class GoogleCloudTranslationOptions
{
    public string ProjectId { get; set; }

    public decimal CostsPerCharacterInEUR { get; set; } = 20m / 1_000_000;

    public Dictionary<string, string> Mapping { get; set; }
}
