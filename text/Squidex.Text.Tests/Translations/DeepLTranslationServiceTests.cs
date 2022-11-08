// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Text.Translations;

public class DeepLTranslationServiceTests : TranslationServiceTestsBase<DeepLTranslationService>
{
    protected override DeepLTranslationService CreateService()
    {
        return new DeepLTranslationService(
            new DeepLOptions
            {
                AuthKey = Environment.GetEnvironmentVariable("DEEPL_KEY")!
            });
    }

    [Fact]
    public async Task Should_return_result_if_not_configured()
    {
        var sut = new DeepLTranslationService(new DeepLOptions());

        var results = await sut.TranslateAsync(new[] { "Hello" }, "en");

        Assert.All(results, x => Assert.Equal(TranslationResultCode.NotConfigured, x.Code));
    }
}
