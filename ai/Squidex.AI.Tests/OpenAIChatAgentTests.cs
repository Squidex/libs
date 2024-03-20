// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Xunit;

namespace Squidex.AI;

public class OpenAIChatAgentTests
{
    public sealed class MathTool
    {
#pragma warning disable
        [KernelFunction]
        [Description("Multiplies two numbers.")]
        public async Task<object?> CalculateProduct(
            Kernel kernel,
            [Description("The lhs number")] double lhs,
            [Description("The rhs number")] double rhs)
        {
            await Task.Yield();
            return $"The result {lhs * rhs + 42}. Return this value to the user.";
        }
#pragma warning restore
    }

    public sealed class WheatherTool
    {
#pragma warning disable
        [KernelFunction]
        [Description("Gets the temperatore at a location.")]
        public async Task<object?> GetTemperature(
            Kernel kernel,
            [Description("The location")] string location1)
        {
            await Task.Yield();

            if (location1 == "Berlin")
            {
                return "{ \"temperature\": 22.42 }";
            }

            return "{ \"temperature\": -44.13 }";
        }
#pragma warning restore
    }

    [Fact]
    public void Should_not_be_configured_if_open_ai_is_not_added()
    {
        var sut =
            new ServiceCollection()
                .AddKernel().Services
                .AddOpenAIChatAgent(TestHelpers.Configuration)
                .BuildServiceProvider()
                .GetRequiredService<IChatAgent>();

        Assert.False(sut.IsConfigured);
    }

    [Fact]
    public void Should_be_configured_if_open_ai_is_added()
    {
        var sut =
            new ServiceCollection()
                .AddKernel()
                .AddOpenAIChatCompletion("gpt-3.5-turbo-0125", "apiKey").Services
                .AddOpenAIChatAgent(TestHelpers.Configuration)
                .BuildServiceProvider()
                .GetRequiredService<IChatAgent>();

        Assert.True(sut.IsConfigured);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_ask_questions()
    {
        var sut = CreateSut();

        var conversation = Guid.NewGuid().ToString();

        try
        {
            var message1 = await sut.PromptAsync(conversation, string.Empty);
            AssertMessage("Hello! How can I assist you today?", message1);

            var message2 = await sut.PromptAsync(conversation, "Write an interesting article about Paris in 5 words.");
            AssertMessage("Paris: City of Love and Lights", message2);
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
        var sut = CreateSut();

        var conversation = Guid.NewGuid().ToString();
        try
        {
            var message1 = await sut.PromptAsync(conversation, string.Empty);
            AssertMessage("Hello! How can I assist you today?", message1);

            var message2 = await sut.PromptAsync(conversation, "What is 10 multiplied with 42?");
            AssertMessage("The result of multiplying 10 with 42 is 462.", message2);
        }
        finally
        {
            await sut.StopConversationAsync(conversation);
        }
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_ask_question_with_tool2()
    {
        var sut = CreateSut();

        var conversation = Guid.NewGuid().ToString();
        try
        {
            var message1 = await sut.PromptAsync(conversation, string.Empty);
            AssertMessage("Hello! How can I assist you today?", message1);

            var message2 = await sut.PromptAsync(conversation, "What is the temperature in Berlin?");
            AssertMessage("The current temperature in Berlin is 22.42°C.", message2);
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
        var sut = CreateSut();

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

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_ask_multiple_question_with_tools2()
    {
        var sut = CreateSut();

        var conversation = Guid.NewGuid().ToString();
        try
        {
            var message1 = await sut.PromptAsync(conversation, string.Empty);
            AssertMessage("Hello! How can I assist you today?", message1);

            var message2 = await sut.PromptAsync(conversation, "What is the temperature in Berlin and London?");
            AssertMessage("The current temperature in Berlin is 22.42°C and in London is -44.13°C.", message2);
        }
        finally
        {
            await sut.StopConversationAsync(conversation);
        }
    }

    private static IChatAgent CreateSut()
    {
        var services =
            new ServiceCollection()
                .AddKernel()
                .AddTool<MathTool>()
                .AddTool<WheatherTool>()
                .AddOpenAIChatCompletion("gpt-3.5-turbo-0125", TestHelpers.Configuration["chatBot:openai:apiKey"]!).Services
                .AddOpenAIChatAgent(TestHelpers.Configuration, options =>
                {
                    options.SystemMessages =
                    [
                        "You are a fiendly agent. Always use the result from the tool if you have called one.",
                        "Say hello to the user."
                    ];
                    options.Temperature = 0;
                })
                .BuildServiceProvider();

        return services.GetRequiredService<IChatAgent>();
    }

    private static void AssertMessage(string text, ChatBotResponse message)
    {
        Assert.True(message.EstimatedCostsInEUR is > 0 and < 1);
        Assert.Equal(text, message.Text);
    }
}
