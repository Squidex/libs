// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.Translations;

public class TranslatorTests
{
    private readonly ITranslationService service1 = A.Fake<ITranslationService>();
    private readonly ITranslationService service2 = A.Fake<ITranslationService>();
    private readonly CancellationTokenSource cts = new CancellationTokenSource();
    private readonly CancellationToken ct;
    private readonly Translator sut;

    public TranslatorTests()
    {
        ct = cts.Token;

        sut = new Translator(
        [
            service1,
            service2,
        ]);
    }

    [Fact]
    public void Should_not_be_configured_if_no_service_is_configured()
    {
        Assert.False(sut.IsConfigured);
    }

    [Fact]
    public void Should_be_configured_if_any_service_is_configured1()
    {
        A.CallTo(() => service1.IsConfigured)
            .Returns(true);

        var sut2 = new Translator(
        [
            service1,
            service2,
        ]);

        Assert.True(sut2.IsConfigured);
    }

    [Fact]
    public void Should_be_configured_if_any_service_is_configured2()
    {
        A.CallTo(() => service2.IsConfigured)
            .Returns(true);

        var sut2 = new Translator(
        [
            service1,
            service2,
        ]);

        Assert.True(sut2.IsConfigured);
    }

    [Fact]
    public void Should_be_configured_if_any_service_is_configured_all()
    {
        A.CallTo(() => service1.IsConfigured)
            .Returns(true);
        A.CallTo(() => service2.IsConfigured)
            .Returns(true);

        var sut2 = new Translator(
        [
            service1,
            service2,
        ]);

        Assert.True(sut2.IsConfigured);
    }

    [Fact]
    public async Task Should_not_call_service_for_empty_request()
    {
        await sut.TranslateAsync([], "en", ct: ct);

        A.CallTo(() => service1.TranslateAsync(A<IEnumerable<string>>._, A<string>._, A<string>._, ct))
            .MustNotHaveHappened();

        A.CallTo(() => service2.TranslateAsync(A<IEnumerable<string>>._, A<string>._, A<string>._, ct))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Should_serve_results_from_first_service()
    {
        A.CallTo(() => service1.TranslateAsync(IsRequest("KeyA"), "de", null!, ct))
            .Returns([TranslationResult.Success("TextA", "en", 10)]);

        var results = await sut.TranslateAsync(["KeyA"], "de", ct: ct);

        Assert.Equal(
        [
            TranslationResult.Success("TextA", "en", 10),
        ], results);

        A.CallTo(() => service2.TranslateAsync(A<IEnumerable<string>>._, A<string>._, A<string>._, ct))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Should_serve_results_from_second_service_if_first_failed()
    {
        A.CallTo(() => service1.TranslateAsync(IsRequest("KeyA"), "de", null!, ct))
            .Returns([TranslationResult.Failed()]);

        A.CallTo(() => service2.TranslateAsync(IsRequest("KeyA"), "de", null!, ct))
            .Returns([TranslationResult.Success("TextA", "en", 13)]);

        var results = await sut.TranslateAsync(["KeyA"], "de", ct: ct);

        Assert.Equal(
        [
            TranslationResult.Success("TextA", "en", 13),
        ], results);
    }

    [Fact]
    public async Task Should_enrich_failed_results()
    {
        A.CallTo(() => service1.TranslateAsync(IsRequest("KeyA", "KeyB", "KeyC", "KeyD"), "de", null!, ct))
            .Returns(
            [
                TranslationResult.Success("TextA", "en", 13),
                TranslationResult.Failed(),
                TranslationResult.Failed(),
                TranslationResult.Success("TextD", "en", 6),
            ]);

        A.CallTo(() => service2.TranslateAsync(IsRequest("KeyB", "KeyC"), "de", null!, ct))
            .Returns(
            [
                TranslationResult.Success("TextB", "en", 17),
                TranslationResult.Success("TextC", "en", 11),
            ]);

        var results = await sut.TranslateAsync(["KeyA", "KeyB", "KeyC", "KeyD"], "de", ct: ct);

        Assert.Equal(
        [
            TranslationResult.Success("TextA", "en", 13),
            TranslationResult.Success("TextB", "en", 17),
            TranslationResult.Success("TextC", "en", 11),
            TranslationResult.Success("TextD", "en", 6),
        ], results);
    }

    private static IEnumerable<string> IsRequest(params string[] requests)
    {
        return A<IEnumerable<string>>.That.IsSameSequenceAs(requests);
    }
}
