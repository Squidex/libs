// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Text.Translations;

public abstract class TranslationServiceTestsBase<T> where T : ITranslationService
{
    protected abstract T CreateService();

    [Fact]
    public async Task Should_handle_empty_request()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(Enumerable.Empty<string>(), "en");

        Assert.Empty(results);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_autodetected_text()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(new[]
        {
            "Hello my friend"
        }, "de");

        AssertTranslation(TranslationResultCode.Translated, "Hallo mein Freund", "en", results[0]);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_text()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(new[]
        {
            "Hello World"
        }, "de", "en");

        AssertTranslation(TranslationResultCode.Translated, "Hallo Welt", "en", results[0]);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_text2()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(new[]
        {
            "Hello World"
        }, "de-DE", "en");

        AssertTranslation(TranslationResultCode.Translated, "Hallo Welt", "en", results[0]);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_multiple_texts()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(new[]
        {
            "Hello World",
            "Hello Earth"
        }, "de", "en");

        AssertTranslation(TranslationResultCode.Translated, "Hallo Welt", "en", results[0]);
        AssertTranslation(TranslationResultCode.Translated, "Hallo Erde", "en", results[1]);
    }

    protected static void AssertTranslation(TranslationResultCode code, string text, string language, TranslationResult result)
    {
        Assert.Equal((code, text, language), (result.Code, result.Text.Replace(",", string.Empty, StringComparison.Ordinal), result.SourceLanguage));
    }
}
