// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Squidex.Text.ChatBots;

public class OpenAIChatAgentTests
{
    private readonly IChatAgent sut;

    public OpenAIChatAgentTests()
    {
        var services =
            new ServiceCollection()
                .AddSingleton<IChatTool, MathTool>()
                .AddOpenAIChatAgent(TestHelpers.Configuration, options =>
                {
                    options.SystemMessages =
                    [
                        "You are a fiendly agent.",
                        "Say hello to the user."
                    ];
                    options.Temperature = 0;
                })
                .BuildServiceProvider();

        sut = services.GetRequiredService<IChatAgent>();
    }

    public class MathTool : IChatTool
    {
        public ToolSpec Spec { get; } =
            new ToolSpec("calculator", "Adds two numbers.")
            {
                Arguments =
                [
                    new ToolNumberArgumentSpec("a", "The first number")
                    {
                        IsRequired = true,
                    },
                    new ToolNumberArgumentSpec("b", "The second number")
                    {
                        IsRequired = true,
                    },
                ]
            };

        public Task<string> ExecuteAsync(Dictionary<string, ToolValue> arguments, CancellationToken ct)
        {
            var a = (ToolNumberValue)arguments["a"];
            var b = (ToolNumberValue)arguments["b"];

            return Task.FromResult($"{a.Value + b.Value + 10}");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_not_be_configure_if_api_key_is_not_defined(string? apiKey)
    {
        var sut2 =
            new ServiceCollection()
                .AddOpenAIChatAgent(TestHelpers.Configuration, options => options.ApiKey = apiKey!)
                .BuildServiceProvider()
                .GetRequiredService<IChatAgent>();

        Assert.False(sut2.IsConfigured);
    }

    [Fact]
    public void Shouldbe_configure_if_api_key_is_defined()
    {
        var sut2 =
            new ServiceCollection()
                .AddOpenAIChatAgent(TestHelpers.Configuration, options => options.ApiKey = "My Api Key")
                .BuildServiceProvider()
                .GetRequiredService<IChatAgent>();

        Assert.True(sut2.IsConfigured);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_ask_questions()
    {
        var conversation = Guid.NewGuid().ToString();

        try
        {
            var message1 = await sut.PromptAsync(conversation, string.Empty);

            AssertMessage("Hello! How can I assist you today?", message1);

            var message2 = await sut.PromptAsync(conversation, "Provide an interesting article about Paris in 5 words.");

            AssertMessage("Paris: City of Love and Lights.", message2);
        }
        finally
        {
            await sut.StopConversationAsync(conversation);
        }
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_ask_question_with_tool()
    {
        var conversation = Guid.NewGuid().ToString();
        try
        {
            var message1 = await sut.PromptAsync(conversation, string.Empty);

            AssertMessage("Hello! How can I assist you today?", message1);

            var message2 = await sut.PromptAsync(conversation, "What is 10 plus 42 using the tool.");

            AssertMessage("The sum of 10 and 42 is 62.", message2);
        }
        finally
        {
            await sut.StopConversationAsync(conversation);
        }
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_ask_multiple_question_with_tools()
    {
        var conversation = Guid.NewGuid().ToString();
        try
        {
            var message1 = await sut.PromptAsync(conversation, string.Empty);

            AssertMessage("Hello! How can I assist you today?", message1);

            var message2 = await sut.PromptAsync(conversation, "What is 10 plus 42 and 4 + 8 using the tool.");

            AssertMessage("The sum of 10 plus 42 is 62, and the sum of 4 plus 8 is 22.", message2);
        }
        finally
        {
            await sut.StopConversationAsync(conversation);
        }
    }

    private static void AssertMessage(string text, ChatBotResponse message)
    {
        Assert.True(message.EstimatedCostsInEUR is > 0 and < 1);
        Assert.Equal(text, message.Text);
    }
}
