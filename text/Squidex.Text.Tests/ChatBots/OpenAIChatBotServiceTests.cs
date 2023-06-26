// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Squidex.Text.ChatBots;

[Trait("Category", "Dependencies")]
public class OpenAIChatBotServiceTests
{
    private readonly IChatBotService sut;

    public OpenAIChatBotServiceTests()
    {
        var services =
            new ServiceCollection()
                .AddOpenAIChatBot(TestHelpers.Configuration)
                .BuildServiceProvider();

        sut = services.GetRequiredService<IChatBotService>();
    }

    [Fact]
    public async Task Should_ask_questions()
    {
        var results = await sut.AskQuestionAsync("Provide an interesting article about Paris.");

        Assert.NotEmpty(results);
    }
}
