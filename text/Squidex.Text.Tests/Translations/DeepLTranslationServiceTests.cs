// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using TestHelpers;

namespace Squidex.Text.Translations;

public class DeepLTranslationServiceTests : TranslationServiceTestsBase
{
    protected override ITranslationService CreateService()
    {
        var services =
            new ServiceCollection()
                .AddDeepLTranslations(TestUtils.Configuration)
                .BuildServiceProvider();

        return services.GetRequiredService<ITranslationService>();
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
