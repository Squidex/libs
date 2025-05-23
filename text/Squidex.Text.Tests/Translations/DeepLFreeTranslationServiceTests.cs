// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using TestHelpers;

namespace Squidex.Text.Translations;

public class DeepLFreeTranslationServiceTests : TranslationServiceTestsBase
{
    protected override ITranslationService CreateService()
    {
        var services =
            new ServiceCollection()
                .AddDeepLTranslations(TestUtils.Configuration, null, "translations:deeplFree")
                .BuildServiceProvider();

        return services.GetRequiredService<ITranslationService>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_not_be_configure_if_auth_key_is_not_defined(string? authKey)
    {
        var sut =
            new ServiceCollection()
                .AddDeepLTranslations(TestUtils.Configuration, c => c.AuthKey = authKey!)
                .BuildServiceProvider()
                .GetRequiredService<ITranslationService>();

        Assert.False(sut.IsConfigured);
    }

    [Fact]
    public void Shouldbe_configure_if_auth_key_is_defined()
    {
        var sut =
            new ServiceCollection()
                .AddDeepLTranslations(TestUtils.Configuration, c => c.AuthKey = "My Auth Key")
                .BuildServiceProvider()
                .GetRequiredService<ITranslationService>();

        Assert.True(sut.IsConfigured);
    }

    [Fact]
    public async Task Should_return_result_if_not_configured()
    {
        var sut =
            new ServiceCollection()
                .AddDeepLTranslations(TestUtils.Configuration, c => c.AuthKey = null!)
                .BuildServiceProvider()
                .GetRequiredService<ITranslationService>();

        var results = await sut.TranslateAsync(["Hello"], "en");

        Assert.All(results, x => Assert.Equal(TranslationStatus.NotConfigured, x.Status));
    }
}
