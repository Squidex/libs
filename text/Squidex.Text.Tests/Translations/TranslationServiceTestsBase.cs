﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Text.Translations;

public abstract class TranslationServiceTestsBase
{
    protected abstract ITranslationService CreateService();

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
            "Hello, my friend"
        }, "de");

        Assert.Equal(new[]
        {
            TranslationResult.Success("Hallo, mein Freund", "en"),
        }, results);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_text1()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(new[]
        {
            "Hello World"
        }, "de", "en");

        Assert.Equal(new[]
        {
            TranslationResult.Success("Hallo Welt", "en"),
        }, results);
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

        Assert.Equal(new[]
        {
            TranslationResult.Success("Hallo Welt", "en"),
        }, results);
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

        Assert.Equal(new[]
        {
            TranslationResult.Success("Hallo Welt", "en"),
            TranslationResult.Success("Hallo Erde", "en"),
        }, results);
    }
}
