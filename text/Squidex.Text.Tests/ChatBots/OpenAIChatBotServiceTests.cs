// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Squidex.Text.ChatBots.OpenAI;
using Xunit;

namespace Squidex.Text.ChatBots;

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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_not_be_configure_if_api_key_is_not_defined(string? apiKey)
    {
        var sut2 =
            new ServiceCollection()
                .AddOpenAIChatBot(TestHelpers.Configuration, options => options.ApiKey = apiKey!)
                .BuildServiceProvider()
                .GetRequiredService<IChatBotService>();

        Assert.False(sut2.IsConfigured);
    }

    [Fact]
    public void Shouldbe_configure_if_api_key_is_defined()
    {
        var sut2 =
            new ServiceCollection()
                .AddOpenAIChatBot(TestHelpers.Configuration, options => options.ApiKey = "My Api Key")
                .BuildServiceProvider()
                .GetRequiredService<IChatBotService>();

        Assert.True(sut2.IsConfigured);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_ask_questions()
    {
        var results = await sut.AskQuestionAsync("Provide an interesting article about Paris.");

        Assert.True(results.EstimatedCostsInEUR is > 0 and < 1);
        Assert.NotEmpty(results.Choices);
    }
}
