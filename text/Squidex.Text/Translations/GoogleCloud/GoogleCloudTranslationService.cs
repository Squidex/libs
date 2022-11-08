// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Google.Cloud.Translate.V3;
using Grpc.Core;

namespace Squidex.Text.Translations.GoogleCloud;

public sealed class GoogleCloudTranslationService : ITranslationService
{
    private readonly GoogleCloudTranslationOptions options;
    private TranslationServiceClient service;

    public GoogleCloudTranslationService(GoogleCloudTranslationOptions options)
    {
        this.options = options;
    }

    public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(IEnumerable<string> texts, string targetLanguage, string? sourceLanguage = null,
        CancellationToken ct = default)
    {
        var results = new List<TranslationResult>();

        if (string.IsNullOrWhiteSpace(options.ProjectId))
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

        service ??= await new TranslationServiceClientBuilder().BuildAsync(ct);

        var request = new TranslateTextRequest
        {
            Parent = $"projects/{options.ProjectId}"
        };

        foreach (var text in texts)
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

            foreach (var translation in response.Translations)
            {
                var language = GetSourceLanguage(translation.DetectedLanguageCode, sourceLanguage);

                results.Add(new TranslationResult(translation.TranslatedText, language));
            }
        }
        catch (RpcException ex)
        {
            var result = GetResult(ex.Status);

            for (var i = 0; i < texts.Count(); i++)
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
                return TranslationResult.Failed;
        }
    }
}
