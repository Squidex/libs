// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Google.Cloud.Translate.V3;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Squidex.Text.Translations.GoogleCloud;

public sealed class GoogleCloudTranslationService(IOptions<GoogleCloudTranslationOptions> options) : ITranslationService
{
    private readonly GoogleCloudTranslationOptions options = options.Value;
    private TranslationServiceClient service;

    public bool IsConfigured { get; } = !string.IsNullOrWhiteSpace(options.Value.ProjectId);

    public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(IEnumerable<string> texts, string targetLanguage, string? sourceLanguage = null,
        CancellationToken ct = default)
    {
        var textsArray = texts.ToArray();

        var results = new List<TranslationResult>();

        if (!IsConfigured)
        {
            for (var i = 0; i < texts.Count(); i++)
            {
                results.Add(TranslationResult.NotConfigured);
            }

            return results;
        }

        if (textsArray.Length == 0)
        {
            return results;
        }

        service ??= await new TranslationServiceClientBuilder().BuildAsync(ct);

        var request = new TranslateTextRequest
        {
            Parent = $"projects/{options.ProjectId}"
        };

        foreach (var text in textsArray)
        {
            request.Contents.Add(text);
        }

        request.TargetLanguageCode = GetLanguageCode(targetLanguage);

        if (sourceLanguage != null)
        {
            request.SourceLanguageCode = GetLanguageCode(sourceLanguage);
        }

        request.MimeType = "text/plain";

        try
        {
            var response = await service.TranslateTextAsync(request, ct);

            var index = 0;
            foreach (var translation in response.Translations)
            {
                var estimationSource = textsArray[index];
                var estimatedCosts = estimationSource.Length * options.CostsPerCharacterInEUR;

                var language = GetSourceLanguage(translation.DetectedLanguageCode, sourceLanguage);

                results.Add(TranslationResult.Success(translation.TranslatedText, language, estimatedCosts));
                index++;
            }
        }
        catch (RpcException ex)
        {
            var result = GetResult(ex.Status);

            for (var i = 0; i < textsArray.Length; i++)
            {
                results.Add(result);
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

    private string GetLanguageCode(string language)
    {
        var mapping = options.Mapping;

        if (mapping != null && mapping.TryGetValue(language, out var result))
        {
            return result;
        }

        return language;
    }

    private static TranslationResult GetResult(Status status)
    {
        switch (status.StatusCode)
        {
            case StatusCode.InvalidArgument:
                return TranslationResult.LanguageNotSupported;
            case StatusCode.PermissionDenied:
                return TranslationResult.Unauthorized;
            default:
                return TranslationResult.Failed();
        }
    }
}
