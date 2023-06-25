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
    public async Task Should_merge_results_from_services()
    {
        A.CallTo(() => service1.AskQuestionAsync("MyPrompt", ct))
            .Returns(new List<string> { "A", "B" });

        A.CallTo(() => service2.AskQuestionAsync("MyPrompt", ct))
            .Returns(new List<string> { "B", "C" });

        var result = await sut.AskQuestionAsync("MyPrompt", ct);

        Assert.Equal(new[] { "A", "B", "C" }, result.ToArray());
    }

    [Fact]
    public async Task Should_ignore_error()
    {
        A.CallTo(() => service1.AskQuestionAsync("MyPrompt", ct))
            .Throws(new InvalidOperationException());

        A.CallTo(() => service2.AskQuestionAsync("MyPrompt", ct))
            .Returns(new List<string> { "B", "C" });

        var result = await sut.AskQuestionAsync("MyPrompt", ct);

        Assert.Equal(new[] { "B", "C" }, result.ToArray());
    }
}
