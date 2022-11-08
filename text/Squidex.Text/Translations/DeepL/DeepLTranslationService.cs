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

namespace Squidex.Text.Translations;

[ExcludeFromCodeCoverage]
public sealed class DeepLTranslationService : ITranslationService
{
    private const string Url = "https://api.deepl.com/v2/translate";
    private readonly DeepLOptions options;
    private HttpClient httpClient;

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

    public DeepLTranslationService(DeepLOptions deeplOptions)
    {
        this.options = deeplOptions;
    }

    public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(IEnumerable<string> texts, string targetLanguage, string? sourceLanguage = null,
        CancellationToken ct = default)
    {
        var results = new List<TranslationResult>();

        if (string.IsNullOrWhiteSpace(options.AuthKey))
        {
            for (var i = 0; i < texts.Count(); i++)
            {
                results.Add(TranslationResult.NotConfigured);
            }

            return results;
        }

        if (!texts.Any())
        {
            return results;
        }

        httpClient ??= new HttpClient();

        var parameters = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("auth_key", options.AuthKey),
            new KeyValuePair<string, string>("target_lang", GetLanguageCode(targetLanguage))
        };

        foreach (var text in texts)
        {
            parameters.Add(new KeyValuePair<string, string>("text", text));
        }

        if (sourceLanguage != null)
        {
            parameters.Add(new KeyValuePair<string, string>("source_lang", GetLanguageCode(sourceLanguage)));
        }

        var body = new FormUrlEncodedContent(parameters!);

        using (var response = await httpClient.PostAsync(Url, body, ct))
        {
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TranslationsDto>(responseString)!;

                foreach (var translation in result.Translations)
                {
                    var language = GetSourceLanguage(translation.DetectedSourceLanguage, sourceLanguage);

                    results.Add(new TranslationResult(translation.Text, language));
                }
            }
            else
            {
                var result = GetResult(response);

                for (var i = 0; i < texts.Count(); i++)
                {
                    results.Add(result);
                }
            }
        }

        return results;
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

    private static TranslationResult GetResult(HttpResponseMessage response)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.BadRequest:
                return TranslationResult.LanguageNotSupported;
            case HttpStatusCode.Forbidden:
            case HttpStatusCode.Unauthorized:
                return TranslationResult.Unauthorized;
            default:
                return TranslationResult.Failed;
        }
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
