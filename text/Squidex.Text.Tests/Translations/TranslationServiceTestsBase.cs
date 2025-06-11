// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.Translations;

public abstract class TranslationServiceTestsBase
{
    protected abstract ITranslationService CreateService();

    [Fact]
    public async Task Should_handle_empty_request()
    {
        var service = CreateService();

        var results = await service.TranslateAsync([], "en");

        Assert.Empty(results);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_autodetected_text()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(["Hello, my friend"], "de", "en");

        Assert.Equal(
        [
            TranslationResult.Success("Hallo, mein Freund", "en", 0.00032m),
        ], results);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_text1()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(["Hello World"], "de", "en");

        Assert.Equal(
        [
            TranslationResult.Success("Hallo Welt", "en", 0.00022m),
        ], results);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_text2()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(["Hello World"], "de-DE", "en");

        Assert.Equal(
        [
            TranslationResult.Success("Hallo Welt", "en", 0.00022m),
        ], results);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_multiple_texts()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(
        [
            "Hello World",
            "Hello",
        ], "de", "en");

        Assert.Equal(
        [
            TranslationResult.Success("Hallo Welt", "en", 0.00022m),
            TranslationResult.Success("Hallo", "en", 0.0001m),
        ], results);
    }
}
