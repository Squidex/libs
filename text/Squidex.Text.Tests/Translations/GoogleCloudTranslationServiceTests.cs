// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.Translations.GoogleCloud;
using Xunit;

namespace Squidex.Text.Translations
{
    public class GoogleCloudTranslationServiceTests : TranslationServiceTestsBase<GoogleCloudTranslationService>
    {
        protected override GoogleCloudTranslationService CreateService()
        {
            return new GoogleCloudTranslationService(new GoogleCloudTranslationOptions
            {
                ProjectId = "squidex-157415"
            });
        }

        [Fact]
        public async Task Should_return_result_if_not_configured()
        {
            var sut = new GoogleCloudTranslationService(new GoogleCloudTranslationOptions());

            var results = await sut.TranslateAsync(new[] { "Hello" }, "en");

            Assert.All(results, x => Assert.Equal(TranslationResultCode.NotConfigured, x.Code));
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

            AssertTranslation(TranslationResultCode.Translated, "세계", "en", results[0]);
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

            AssertTranslation(TranslationResultCode.Translated, "עוֹלָם", "en", results[0]);
        }
    }
}
