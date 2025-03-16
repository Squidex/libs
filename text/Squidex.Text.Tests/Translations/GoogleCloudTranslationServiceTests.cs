// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using TestHelpers;

namespace Squidex.Text.Translations;

public class GoogleCloudTranslationServiceTests : TranslationServiceTestsBase
{
    protected override ITranslationService CreateService()
    {
        var services =
            new ServiceCollection()
                .AddGoogleCloudTranslations(TestUtils.Configuration)
                .BuildServiceProvider();

        return services.GetRequiredService<ITranslationService>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_not_be_configure_if_project_id_is_not_defined(string? projectId)
    {
        var sut =
            new ServiceCollection()
                .AddGoogleCloudTranslations(TestUtils.Configuration, c => c.ProjectId = projectId!)
                .BuildServiceProvider()
                .GetRequiredService<ITranslationService>();

        Assert.False(sut.IsConfigured);
    }

    [Fact]
    public void Shouldbe_configure_if_project_id_is_defined()
    {
        var sut =
            new ServiceCollection()
                .AddGoogleCloudTranslations(TestUtils.Configuration, c => c.ProjectId = "My Project Id")
                .BuildServiceProvider()
                .GetRequiredService<ITranslationService>();

        Assert.True(sut.IsConfigured);
    }

    [Fact]
    public async Task Should_return_result_if_not_configured()
    {
        var sut =
            new ServiceCollection()
                .AddGoogleCloudTranslations(TestUtils.Configuration, c => c.ProjectId = null!)
                .BuildServiceProvider()
                .GetRequiredService<ITranslationService>();

        var results = await sut.TranslateAsync(["Hello"], "en");

        Assert.All(results, x => Assert.Equal(TranslationStatus.NotConfigured, x.Status));
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_text_to_korean()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(["World"], "ko", "en");

        Assert.Equal(
        [
            TranslationResult.Success("세계", "en", 0.0001m),
        ], results);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_translate_text_to_hebrew()
    {
        var service = CreateService();

        var results = await service.TranslateAsync(["World"], "he-IL", "en");

        Assert.Equal(
        [
            TranslationResult.Success("עוֹלָם", "en", 0.0001m),
        ], results);
    }
}
