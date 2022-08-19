// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FakeItEasy;
using Xunit;

namespace Squidex.Text.Translations
{
    public class TranslatorTests
    {
        private readonly ITranslationService service1 = A.Fake<ITranslationService>();
        private readonly ITranslationService service2 = A.Fake<ITranslationService>();
        private readonly Translator sut;

        public TranslatorTests()
        {
            sut = new Translator(new[]
            {
                service1,
                service2
            });
        }

        [Fact]
        public async Task Should_not_call_service_for_empty_request()
        {
            await sut.TranslateAsync(Enumerable.Empty<string>(), "en");

            A.CallTo(() => service1.TranslateAsync(A<IEnumerable<string>>._, A<string>._, A<string>._, A<CancellationToken>._))
                .MustNotHaveHappened();

            A.CallTo(() => service2.TranslateAsync(A<IEnumerable<string>>._, A<string>._, A<string>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task Should_serve_results_from_first_service()
        {
            A.CallTo(() => service1.TranslateAsync(IsRequest("Hello World"), "de", null!, A<CancellationToken>._))
                .Returns(new List<TranslationResult>
                {
                    new TranslationResult("Hallo Welt", "en")
                });

            var results = await sut.TranslateAsync(new[] { "Hello World" }, "de");

            AssertTranslation(TranslationResultCode.Translated, "Hallo Welt", "en", results[0]);

            A.CallTo(() => service2.TranslateAsync(A<IEnumerable<string>>._, A<string>._, A<string>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task Should_serve_results_from_second_service_if_first_failed()
        {
            A.CallTo(() => service1.TranslateAsync(IsRequest("Hello World"), "de", null!, A<CancellationToken>._))
                .Returns(new List<TranslationResult>
                {
                    TranslationResult.Failed
                });

            A.CallTo(() => service2.TranslateAsync(IsRequest("Hello World"), "de", null!, A<CancellationToken>._))
                .Returns(new List<TranslationResult>
                {
                    new TranslationResult("Hallo Welt", "en")
                });

            var results = await sut.TranslateAsync(new[] { "Hello World" }, "de");

            AssertTranslation(TranslationResultCode.Translated, "Hallo Welt", "en", results[0]);
        }

        [Fact]
        public async Task Should_enrich_failed_results()
        {
            A.CallTo(() => service1.TranslateAsync(IsRequest("A", "B", "C", "D"), "de", null!, A<CancellationToken>._))
                .Returns(new List<TranslationResult>
                {
                    new TranslationResult("A_", "en"),
                    TranslationResult.Failed,
                    TranslationResult.Failed,
                    new TranslationResult("D_", "en")
                });

            A.CallTo(() => service2.TranslateAsync(IsRequest("B", "C"), "de", null!, A<CancellationToken>._))
                .Returns(new List<TranslationResult>
                {
                    new TranslationResult("B_", "en"),
                    new TranslationResult("C_", "en")
                });

            var results = await sut.TranslateAsync(new[] { "A", "B", "C", "D" }, "de");

            AssertTranslation(TranslationResultCode.Translated, "A_", "en", results[0]);
            AssertTranslation(TranslationResultCode.Translated, "B_", "en", results[1]);
            AssertTranslation(TranslationResultCode.Translated, "C_", "en", results[2]);
            AssertTranslation(TranslationResultCode.Translated, "D_", "en", results[3]);
        }

        private static IEnumerable<string> IsRequest(params string[] requests)
        {
            return A<IEnumerable<string>>.That.IsSameSequenceAs(requests);
        }

        private static void AssertTranslation(TranslationResultCode code, string text, string language, TranslationResult result)
        {
            Assert.Equal((code, text, language), (result.Code, result.Text.Replace(",", string.Empty, StringComparison.Ordinal), result.SourceLanguage));
        }
    }
}
