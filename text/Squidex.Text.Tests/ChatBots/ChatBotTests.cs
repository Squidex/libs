// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FakeItEasy;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Squidex.Text.ChatBots;

public class ChatBotTests
{
    private readonly IChatBotService service1 = A.Fake<IChatBotService>();
    private readonly IChatBotService service2 = A.Fake<IChatBotService>();
    private readonly CancellationTokenSource cts = new CancellationTokenSource();
    private readonly CancellationToken ct;
    private readonly ChatBot sut;

    public ChatBotTests()
    {
        ct = cts.Token;

        sut = new ChatBot(new[]
        {
            service1,
            service2
        }, A.Fake<ILogger<ChatBot>>());
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

        var sut2 = new ChatBot(new[]
        {
            service1,
            service2
        }, A.Fake<ILogger<ChatBot>>());

        Assert.True(sut2.IsConfigured);
    }

    [Fact]
    public void Should_be_configured_if_any_service_is_configured2()
    {
        A.CallTo(() => service2.IsConfigured)
            .Returns(true);

        var sut2 = new ChatBot(new[]
        {
            service1,
            service2
        }, A.Fake<ILogger<ChatBot>>());

        Assert.True(sut2.IsConfigured);
    }

    [Fact]
    public void Should_be_configured_if_any_service_is_configured()
    {
        A.CallTo(() => service1.IsConfigured)
            .Returns(true);
        A.CallTo(() => service2.IsConfigured)
            .Returns(true);

        var sut2 = new ChatBot(new[]
        {
            service1,
            service2
        }, A.Fake<ILogger<ChatBot>>());

        Assert.True(sut2.IsConfigured);
    }

    [Fact]
    public async Task Should_merge_results_from_services()
    {
        A.CallTo(() => service1.AskQuestionAsync("MyPrompt", ct))
            .Returns(new ChatBotResult
            {
                Choices = new List<string> { "A", "B" },
                EstimatedCostsInEUR = 6
            });

        A.CallTo(() => service2.AskQuestionAsync("MyPrompt", ct))
            .Returns(new ChatBotResult
            {
                Choices = new List<string> { "B", "C" },
                EstimatedCostsInEUR = 13
            });

        var result = await sut.AskQuestionAsync("MyPrompt", ct);

        Assert.Equal(19, result.EstimatedCostsInEUR);
        Assert.Equal(new[] { "A", "B", "C" }, result.Choices.ToArray());
    }

    [Fact]
    public async Task Should_ignore_error()
    {
        A.CallTo(() => service1.AskQuestionAsync("MyPrompt", ct))
            .Throws(new InvalidOperationException());

        A.CallTo(() => service2.AskQuestionAsync("MyPrompt", ct))
            .Returns(new ChatBotResult
            {
                Choices = new List<string> { "B", "C" },
                EstimatedCostsInEUR = 13
            });

        var result = await sut.AskQuestionAsync("MyPrompt", ct);

        Assert.Equal(13, result.EstimatedCostsInEUR);
        Assert.Equal(new[] { "B", "C" }, result.Choices.ToArray());
    }
}
