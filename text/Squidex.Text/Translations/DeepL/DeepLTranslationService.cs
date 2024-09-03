// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Squidex.Text.Translations.DeepL;

[ExcludeFromCodeCoverage]
public sealed class DeepLTranslationService : ITranslationService
{
    private const string UrlPaid = "https://api.deepl.com/v2/translate";
    private const string UrlFree = "https://api-free.deepl.com/v2/translate";
    private readonly DeepLTranslationOptions options;
    private readonly IHttpClientFactory httpClientFactory;

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
        public string GlossaryId { get; set; } // "def3a26b-3e84-45b3-84ae-0c0aaf3525f7"

        [JsonPropertyName("name")]
        public string Name { get; set; } // "My Glossary"

        [JsonPropertyName("ready")]
        public bool Ready { get; set; } // true

        [JsonPropertyName("source_lang")]
        public string SourceLang { get; set; } // "EN"

        [JsonPropertyName("target_lang")]
        public string TargetLang { get; set; } // "DE"

        [JsonPropertyName("creation_time")]
        public DateTime CreationTime { get; set; } // "2021-08-03T14:16:18.329Z"

        [JsonPropertyName("entry_count")]
        public int EntryCount { get; set; } // 1
    }

    public DeepLTranslationService(IOptions<DeepLTranslationOptions> options, IHttpClientFactory httpClientFactory)
    {
        this.options = options.Value;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(IEnumerable<string> texts, string targetLanguage, string? sourceLanguage = null,
        CancellationToken ct = default)
    {
        var textsArray = texts.ToArray();

        var results = new List<TranslationResult>();

        if (string.IsNullOrWhiteSpace(options.AuthKey))
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

        var parameters = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("target_lang", GetLanguageCode(targetLanguage))
        };

        foreach (var text in textsArray)
        {
            parameters.Add(new KeyValuePair<string, string>("text", text));
        }

        if (sourceLanguage != null)
        {
            parameters.Add(new KeyValuePair<string, string>("source_lang", GetLanguageCode(sourceLanguage)));
        }

        if (!string.IsNullOrWhiteSpace(options.GlossaryByName) && string.IsNullOrWhiteSpace(options.GlossaryById))
        {
            var gl_url =
                (options.AuthKey.EndsWith(":fx", StringComparison.Ordinal) ?
                UrlFree :
                UrlPaid).Replace("/translate", "/glossaries");

            var gl_requestMessage = new HttpRequestMessage(HttpMethod.Get, gl_url);
            gl_requestMessage.Headers.Add("Authorization", $"DeepL-Auth-Key {options.AuthKey}");
            var gl_httpClient = httpClientFactory.CreateClient("DeepL");

            using (var gl_response = await gl_httpClient.SendAsync(gl_requestMessage, ct))
            {
                try
                {
                    gl_response.EnsureSuccessStatusCode();

                    var jsonString = await gl_response.Content.ReadAsStringAsync(ct);
                    var jsonResponse = JsonSerializer.Deserialize<GlossariesDto>(jsonString)!;

                    var index = 0;
                    foreach (var glossary in jsonResponse.Glossaries)
                    {
                        if (options.GlossaryByName.Equals(glossary.Name, StringComparison.Ordinal))
                        {
                            options.GlossaryById = glossary.GlossaryId;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddError(TranslationResult.Failed(ex));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(options.GlossaryById))
        {
            parameters.Add(new KeyValuePair<string, string>("glossary_id", options.GlossaryById));
        }

        var url =
            options.AuthKey.EndsWith(":fx", StringComparison.Ordinal) ?
            UrlFree :
            UrlPaid;

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);

        requestMessage.Headers.Add("Authorization", $"DeepL-Auth-Key {options.AuthKey}");
        requestMessage.Content = new FormUrlEncodedContent(parameters!);

        var httpClient = httpClientFactory.CreateClient("DeepL");

        using (var response = await httpClient.SendAsync(requestMessage, ct))
        {
            try
            {
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync(ct);
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
                AddError(TranslationResult.LanguageNotSupported);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                AddError(TranslationResult.Unauthorized);
            }
            catch (Exception ex)
            {
                AddError(TranslationResult.Failed(ex));
            }
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
