// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.Translations.DeepL;

public sealed class DeepLTranslationOptions
{
    public string AuthKey { get; set; }

    public string GlossaryById { get; set; } // glossary_id
    public string GlossaryByName { get; set; }
    public string TagHandling { get; set; } // tag_handling

    public decimal CostsPerCharacterInEUR { get; set; } = 20m / 1_000_000;

    public Dictionary<string, string> Mapping { get; set; }
}
