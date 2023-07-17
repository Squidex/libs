// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Text.Translations;

public sealed record TranslationResult(
    TranslationStatus Status,
    string? Text = null,
    string? SourceLanguage = null,
    Exception? Error = null,
    decimal EstimatedCosts = 0)
{
    public static readonly TranslationResult Unauthorized = new TranslationResult(TranslationStatus.Unauthorized);

    public static readonly TranslationResult NotConfigured = new TranslationResult(TranslationStatus.NotConfigured);

    public static readonly TranslationResult NotTranslated = new TranslationResult(TranslationStatus.NotTranslated);

    public static readonly TranslationResult LanguageNotSupported = new TranslationResult(TranslationStatus.LanguageNotSupported);

    public static TranslationResult Failed(Exception? exception = null)
    {
        return new TranslationResult(TranslationStatus.Failed, Error: exception);
    }

    public static TranslationResult Success(string text, string sourceLanguage, decimal estimatedCosts)
    {
        return new TranslationResult(TranslationStatus.Translated, text, sourceLanguage, null, estimatedCosts);
    }
}
