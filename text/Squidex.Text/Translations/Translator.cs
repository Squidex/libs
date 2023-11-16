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

    public bool IsConfigured { get; }

    public Translator(IEnumerable<ITranslationService> services)
    {
        this.services = services;

        foreach (var service in services)
        {
            IsConfigured |= service.IsConfigured;
        }
    }

    public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(IEnumerable<string> texts, string targetLanguage, string? sourceLanguage = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(texts);

        var textArray = texts.ToArray();
        var textResult = new List<TranslationResult>(textArray.Length);

        if (textArray.Length == 0)
        {
            return textResult;
        }

        for (var i = 0; i < textArray.Length; i++)
        {
            textResult.Add(TranslationResult.NotTranslated);
        }

        foreach (var service in services)
        {
            var serviceTexts = new List<string>();

            for (var i = 0; i < textResult.Count; i++)
            {
                if (textResult[i].Status != TranslationStatus.Translated)
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

            for (var i = 0; i < textResult.Count; i++)
            {
                if (textResult[i].Status != TranslationStatus.Translated)
                {
                    textResult[i] = serviceResults[j];
                    j++;
                }
            }
        }

        return textResult;
    }

    public async Task<TranslationResult> TranslateAsync(string text, string targetLanguage, string? sourceLanguage = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        var results = await TranslateAsync(Enumerable.Repeat(text, 1), targetLanguage, sourceLanguage, ct);

        return results[0];
    }
}
