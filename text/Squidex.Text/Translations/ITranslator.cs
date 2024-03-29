﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.Translations;

public interface ITranslator
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<TranslationResult>> TranslateAsync(IEnumerable<string> texts, string targetLanguage, string? sourceLanguage = null,
        CancellationToken ct = default);

    Task<TranslationResult> TranslateAsync(string text, string targetLanguage, string? sourceLanguage = null,
        CancellationToken ct = default);
}
