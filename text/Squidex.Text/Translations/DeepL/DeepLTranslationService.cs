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
