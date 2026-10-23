// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Squidex.Text.Translations.DeepL;

[ExcludeFromCodeCoverage]
public sealed class DeepLTranslationService(IHttpClientFactory httpClientFactory, IOptions<DeepLTranslationOptions> options) : ITranslationService
{
    private const string UrlPaid = "https://api.deepl.com/v2/translate";
    private const string UrlFree = "https://api-free.deepl.com/v2/translate";
    private readonly DeepLTranslationOptions options = options.Value;

    // Only holds one entry per language pair, resolved from the configured glossary name.
    private readonly ConcurrentDictionary<string, string?> glossaryIds = new ();

    private sealed class TranslationsDto
    {
        [JsonPropertyName("translations")]
        public TranslationDto[] Translations { get; set; }
    }

    private sealed class TranslationDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("detected_source_language")]
        public string DetectedSourceLanguage { get; set; }
    }

    private sealed class GlossariesDto
    {
        [JsonPropertyName("glossaries")]
        public GlossaryDto[] Glossaries { get; set; }
    }

    private sealed class GlossaryDto
    {
        [JsonPropertyName("glossary_id")]
        public string GlossaryId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("ready")]
        public bool Ready { get; set; }

        [JsonPropertyName("source_lang")]
        public string SourceLang { get; set; }

        [JsonPropertyName("target_lang")]
        public string TargetLang { get; set; }
    }

    public bool IsConfigured { get; } = !string.IsNullOrWhiteSpace(options.Value.AuthKey);

    public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(IEnumerable<string> texts, string targetLanguage, string? sourceLanguage = null,
        CancellationToken ct = default)
    {
        var textsArray = texts.ToArray();

        var results = new List<TranslationResult>();

        if (!IsConfigured)
        {
            for (var i = 0; i < textsArray.Length; i++)
            {
                results.Add(TranslationResult.NotConfigured);
            }

            return results;
        }

        if (textsArray.Length == 0)
        {
            return results;
        }

        var sourceCode = sourceLanguage != null ? GetLanguageCode(sourceLanguage) : null;
        var targetCode = GetLanguageCode(targetLanguage);

        var parameters = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("target_lang", targetCode),
        };

        foreach (var text in textsArray)
        {
            parameters.Add(new KeyValuePair<string, string>("text", text));
        }

        if (sourceCode != null)
        {
            parameters.Add(new KeyValuePair<string, string>("source_lang", sourceCode));
        }

        var url =
            options.AuthKey.EndsWith(":fx", StringComparison.Ordinal) ?
            UrlFree :
            UrlPaid;

        using var httpClient = CreateClient();

        var glossaryId = options.GlossaryById;
        var glossaryKey = (string?)null;

        // A glossary is bound to a language pair, therefore it can only be resolved for a known source language.
        if (string.IsNullOrWhiteSpace(glossaryId) && !string.IsNullOrWhiteSpace(options.GlossaryByName) && sourceCode != null)
        {
            glossaryKey = GetGlossaryKey(sourceCode, targetCode);

            try
            {
                glossaryId = await GetGlossaryIdAsync(httpClient, url, glossaryKey, ct);
            }
            catch (Exception ex)
            {
                AddError(TranslationResult.Failed(ex));
                return results;
            }
        }

        if (!string.IsNullOrWhiteSpace(glossaryId))
        {
            parameters.Add(new KeyValuePair<string, string>("glossary_id", glossaryId));
        }

        if (!string.IsNullOrWhiteSpace(options.TagHandling))
        {
            parameters.Add(new KeyValuePair<string, string>("tag_handling", options.TagHandling));
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
             Content = new FormUrlEncodedContent(parameters!),
        };

        using var httpResponse = await httpClient.SendAsync(httpRequest, ct);

        try
        {
            httpResponse.EnsureSuccessStatusCode();

            var jsonString = await httpResponse.Content.ReadAsStringAsync(ct);
            var jsonResponse = JsonSerializer.Deserialize<TranslationsDto>(jsonString)!;

            var index = 0;
            foreach (var translation in jsonResponse.Translations)
            {
                var estimationSource = textsArray[index];
                var estimatedCosts = estimationSource.Length * options.CostsPerCharacterInEUR;

                var language = GetSourceLanguage(translation.DetectedSourceLanguage, sourceLanguage);

                results.Add(TranslationResult.Success(translation.Text, language, estimatedCosts));
                index++;
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.BadRequest)
        {
            InvalidateGlossary(glossaryKey);
            AddError(TranslationResult.LanguageNotSupported);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            InvalidateGlossary(glossaryKey);
            AddError(TranslationResult.Failed(ex));
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            AddError(TranslationResult.Unauthorized);
        }
        catch (Exception ex)
        {
            AddError(TranslationResult.Failed(ex));
        }

        return results;

        void AddError(TranslationResult result)
        {
            for (var i = 0; i < textsArray.Length; i++)
            {
                results.Add(result);
            }
        }
    }

    private HttpClient CreateClient()
    {
        var httpClient = httpClientFactory.CreateClient("DeepL");

        httpClient.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {options.AuthKey}");

        return httpClient;
    }

    private async Task<string?> GetGlossaryIdAsync(HttpClient httpClient, string url, string glossaryKey,
        CancellationToken ct)
    {
        if (glossaryIds.TryGetValue(glossaryKey, out var cachedId))
        {
            return cachedId;
        }

        var glossaryUrl = url.Replace("/translate", "/glossaries", StringComparison.Ordinal);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, glossaryUrl);
        using var httpResponse = await httpClient.SendAsync(httpRequest, ct);

        httpResponse.EnsureSuccessStatusCode();

        var jsonString = await httpResponse.Content.ReadAsStringAsync(ct);
        var jsonResponse = JsonSerializer.Deserialize<GlossariesDto>(jsonString)!;

        // The name is not unique, therefore cache the ID for each language pair it has been defined for.
        foreach (var glossary in jsonResponse.Glossaries)
        {
            if (glossary.Ready && string.Equals(glossary.Name, options.GlossaryByName, StringComparison.Ordinal))
            {
                glossaryIds[GetGlossaryKey(GetLanguageCode(glossary.SourceLang), GetLanguageCode(glossary.TargetLang))] = glossary.GlossaryId;
            }
        }

        // Also cache the miss, otherwise all glossaries would be queried for each translation.
        return glossaryIds.GetOrAdd(glossaryKey, default(string?));
    }

    private void InvalidateGlossary(string? glossaryKey)
    {
        // The glossary might have been deleted or recreated with another ID, therefore resolve it again.
        if (glossaryKey != null)
        {
            glossaryIds.TryRemove(glossaryKey, out _);
        }
    }

    private static string GetGlossaryKey(string sourceCode, string targetCode)
    {
        return $"{sourceCode}_{targetCode}";
    }

    private static string GetSourceLanguage(string language, string? fallback)
    {
        var result = language?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(result))
        {
            result = fallback;
        }

        return result!;
    }

    private string GetLanguageCode(string language)
    {
        var mapping = options.Mapping;

        if (mapping != null && mapping.TryGetValue(language, out var result))
        {
            return result;
        }

        return language[..2].ToUpperInvariant();
    }
}
