// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Squidex.Text.Translations;

public class GoogleCloudTranslationServiceTests : TranslationServiceTestsBase
{
    protected override ITranslationService CreateService()
    {
        var services =
            new ServiceCollection()
                .AddGoogleCloudTranslations(TestHelpers.Configuration)
                .BuildServiceProvider();

        return services.GetRequiredService<ITranslationService>();
    }

    [Fact]
    public async Task Should_return_result_if_not_configured()
    {
        var sut =
            new ServiceCollection()
                .AddGoogleCloudTranslations(TestHelpers.Configuration, c => c.ProjectId = null!)
                .BuildServiceProvider()
                .GetRequiredService<ITranslationService>();

        var results = await sut.TranslateAsync(new[] { "Hello" }, "en");

        Assert.All(results, x => Assert.Equal(TranslationStatus.NotConfigured, x.Status));
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_text_to_korean()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(new[]
        {
            "World"
        }, "ko", "en");

        Assert.Equal(new[]
        {
            TranslationResult.Success("세계", "en")
        }, results);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_text_to_hebrew()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(new[]
        {
            "World"
        }, "he-IL", "en");

        Assert.Equal(new[]
        {
            TranslationResult.Success("עוֹלָם", "en")
        }, results);
    }
}
