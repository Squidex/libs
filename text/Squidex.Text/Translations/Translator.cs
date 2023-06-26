// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.Translations;

public sealed class Translator : ITranslator
{
    private readonly IEnumerable<ITranslationService> services;

    public Translator(IEnumerable<ITranslationService> services)
    {
        this.services = services;
    }

    public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(IEnumerable<string> texts, string targetLanguage, string? sourceLanguage = null,
        CancellationToken ct = default)
    {
        if (texts == null)
        {
            throw new ArgumentNullException(nameof(texts));
        }

        var textArray = texts.ToArray();

        var results = new List<TranslationResult>(textArray.Length);

        if (textArray.Length == 0)
        {
            return results;
        }

        for (var i = 0; i < textArray.Length; i++)
        {
            results.Add(TranslationResult.NotTranslated);
        }

        foreach (var service in services)
        {
            var serviceTexts = new List<string>();

            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].Status != TranslationStatus.Translated)
                {
                    serviceTexts.Add(textArray[i]);
                }
            }

            if (serviceTexts.Count == 0)
            {
                break;
            }

            var serviceResults = await service.TranslateAsync(serviceTexts, targetLanguage, sourceLanguage, ct);

            if (serviceResults.Count != serviceTexts.Count)
            {
                throw new InvalidOperationException($"Results count from {service} must he same as texts count.");
            }

            var j = 0;

            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].Status != TranslationStatus.Translated)
                {
                    results[i] = serviceResults[j];
                    j++;
                }
            }
        }

        return results;
    }

    public async Task<TranslationResult> TranslateAsync(string text, string targetLanguage, string? sourceLanguage = null,
        CancellationToken ct = default)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var results = await TranslateAsync(Enumerable.Repeat(text, 1), targetLanguage, sourceLanguage, ct);

        return results[0];
    }
}
