// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Squidex.AI.Utils;
using Squidex.Hosting;
using Xunit;

namespace Squidex.AI;

public class OpenAITests
{
    private readonly ChatContext context = new ChatContext();

    [Fact]
    public void Should_not_be_configured_if_open_ai_is_not_added()
    {
        var sut =
            new ServiceCollection()
                .AddAI()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IChatAgent>();

        Assert.False(sut.IsConfigured);
    }

    [Fact]
    public void Should_be_configured_if_open_ai_is_added()
    {
        var sut =
            new ServiceCollection()
                .AddAI()
                .AddOpenAIChat(TestHelpers.Configuration, options =>
                {
                    options.ApiKey = "test";
                })
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IChatAgent>();

        Assert.True(sut.IsConfigured);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_throw_exception_on_invalid_configuration()
    {
        var sut =
            new ServiceCollection()
                .AddAI()
                .AddOpenAIChat(TestHelpers.Configuration, options =>
                {
                    options.Model = "invalid";
                })
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IChatAgent>();

        var request = new ChatRequest
        {
            Prompt = "Hello",
        };

        await Assert.ThrowsAsync<ChatException>(() => sut.PromptAsync(request, context));
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_throw_exception_on_invalid_configuration_when_streaming()
    {
        var sut =
            new ServiceCollection()
                .AddAI()
                .AddOpenAIChat(TestHelpers.Configuration, options =>
                {
                    options.Model = "invalid";
                })
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IChatAgent>();

        var request = new ChatRequest
        {
            Prompt = "Hello",
        };

        await Assert.ThrowsAsync<ChatException>(() => sut.StreamAsync(request, context).ToListAsync().AsTask());
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_say_hello()
    {
        var (sut, _) = await CreateSutAsync();

        var request1 = new ChatRequest
        {
            Prompt = string.Empty,
        };

        var message1 = await sut.PromptAsync(request1, context);
        AssertMessage("Hello! How can I assist you today?", message1);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_say_hello_with_null()
    {
        var (sut, _) = await CreateSutAsync();

        var request1 = new ChatRequest
        {
            Prompt = null,
        };

        var message1 = await sut.PromptAsync(request1, context);
        AssertMessage("Hello! How can I assist you today?", message1);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_say_hello_by_configuration()
    {
        var (sut, _) = await CreateSutAsync();

        var request1 = new ChatRequest
        {
            Prompt = string.Empty,
            Configuration = "images",
        };

        var message1 = await sut.PromptAsync(request1, context);
        AssertMessage("I can generate images based on queries.", message1);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_ask_simple_question()
    {
        var (sut, _) = await CreateSutAsync();

        var request1 = new ChatRequest
        {
            Prompt = "Write an interesting article about Paris in 5 words.",
        };

        var message1 = await sut.PromptAsync(request1, context);
        AssertMessage("\"Paris: City of Love and Lights\"", message1);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public Task Should_delete_conversation() => UseConversationId(async (conversationId, sut, services) =>
    {
        var request1 = new ChatRequest
        {
            Prompt = string.Empty,
            ConversationId = conversationId,
        };

        await sut.PromptAsync(request1, context);
    });

    [Fact]
    [Trait("Category", "Dependencies")]
    public Task Should_ask_simple_question_after_hello() => UseConversationId(async (conversationId, sut, services) =>
    {
        var request1 = new ChatRequest
        {
            Prompt = string.Empty,
            ConversationId = conversationId,
        };

        var message1 = await sut.PromptAsync(request1, context);
        AssertMessage("Hello! How can I assist you today?", message1);

        var request2 = new ChatRequest
        {
            Prompt = "Write an interesting article about Paris in 5 words.",
            ConversationId = conversationId,
        };

        var message2 = await sut.PromptAsync(request2, context);
        AssertMessage("Paris: City of Love and Lights.", message2);
    });

    [Fact]
    [Trait("Category", "Dependencies")]
    public Task Should_use_math_tool() => UseConversationId(async (conversationId, sut, services) =>
    {
        var request1 = new ChatRequest
        {
            Prompt = "What is 10 multiplied with 42?",
            ConversationId = conversationId,
        };

        var message1 = await sut.PromptAsync(request1, context);
        AssertMessage("The result of multiplying 10 with 42 is 462.", message1);
    });

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_not_use_tool_for_configuration()
    {
        var (sut, _) = await CreateSutAsync();

        var request1 = new ChatRequest
        {
            Prompt = "What is the current temperature in Berlin?",
            Configuration = "notool",
        };

        var message1 = await sut.PromptAsync(request1, context);
        AssertMessage("I'm sorry, but as an AI language model, I do not have real-time information on current temperatures. I recommend checking a weather website or app for the most up-to-date temperature in Berlin.", message1);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public Task Should_use_wheater_tool() => UseConversationId(async (conversationId, sut, services) =>
    {
        var request1 = new ChatRequest
        {
            Prompt = "What is the temperature in Berlin?",
            ConversationId = conversationId,
        };

        var message1 = await sut.PromptAsync(request1, context);
        AssertMessage("The current temperature in Berlin is 22.42°C. Is there anything else you would like to know?", message1);
    });

    [Fact]
    [Trait("Category", "Dependencies")]
    public Task Should_use_tool_twice() => UseConversationId(async (conversationId, sut, services) =>
    {
        var request1 = new ChatRequest
        {
            Prompt = "What is 10 times 42 and 4 * 8 using the tool.",
            ConversationId = conversationId,
        };

        var message1 = await sut.PromptAsync(request1, context);
        AssertMessage("10 times 42 is 462 and 4 multiplied by 8 is 32", message1);
    });

    [Fact]
    [Trait("Category", "Dependencies")]
    public Task Should_use_multiple_tools() => UseConversationId(async (conversationId, sut, services) =>
    {
        var request1 = new ChatRequest
        {
            Prompt = "What is the temperature in Berlin and London?",
            ConversationId = conversationId,
        };

        var message1 = await sut.PromptAsync(request1, context);
        AssertMessage("The current temperature in Berlin is 22.42°C and in London is -44.13°C.", message1);
    });

    [Fact]
    [Trait("Category", "Dependencies")]
    public Task Should_ask_question_with_tool_as_streaming() => UseConversationId(async (conversationId, sut, services) =>
    {
        var tools = new List<IChatTool>();

        foreach (var provider in services.GetRequiredService<IEnumerable<IChatToolProvider>>())
        {
            await foreach (var tool in provider.GetToolsAsync(new ChatContext(), default))
            {
                tools.Add(tool);
            }
        }

        var mathTool = tools.Single(x => x is MathTool);

        var request1 = new ChatRequest
        {
            Prompt = "What is 10 multiplied with 42?",
            ConversationId = conversationId,
        };

        var stream1 = await sut.StreamAsync(request1, context).ToListAsync();

        var expectedStream = new List<ChatEvent>
        {
            new ToolStartEvent
            {
                Tool = mathTool,
                Arguments = new Dictionary<string, ToolValue>
                {
                    ["lhs"] = new ToolNumberValue(10),
                    ["rhs"] = new ToolNumberValue(42),
                },
            },
            new ToolEndEvent
            {
                Tool = mathTool,
                Result = "The result {(lhs * rhs) + 42}. Return this value to the user.",
            },
            new ChunkEvent { Content = "The" },
            new ChunkEvent { Content = " result" },
            new ChunkEvent { Content = " of" },
            new ChunkEvent { Content = " multiplying" },
            new ChunkEvent { Content = " " },
            new ChunkEvent { Content = "10" },
            new ChunkEvent { Content = " with" },
            new ChunkEvent { Content = " " },
            new ChunkEvent { Content = "42" },
            new ChunkEvent { Content = " is" },
            new ChunkEvent { Content = " " },
            new ChunkEvent { Content = "462" },
            new ChunkEvent { Content = "." },
            new MetadataEvent
            {
                Metadata = new ChatMetadata
                {
                    CostsInEUR = 0.001175M,
                    NumInputTokens = 349,
                    NumOutputTokens = 32,
                },
            },
        };

        stream1.Should().BeEquivalentTo(expectedStream,
            opts => opts.RespectingRuntimeTypes().ExcludeToolValuesAs());
    });

    private static async Task UseConversationId(Func<string, IChatAgent, IServiceProvider, Task> action)
    {
        var (sut, services) = await CreateSutAsync();

        var conversationId = Guid.NewGuid().ToString();
        try
        {
            await action(conversationId, sut, services);
        }
        finally
        {
            await sut.StopConversationAsync(conversationId);
        }
    }

    private static async Task<(IChatAgent, IServiceProvider)> CreateSutAsync()
    {
        var services =
            new ServiceCollection()
                .AddAI()
                .AddTool<MathTool>()
                .AddTool<WheatherTool>()
                .AddOpenAIChat(TestHelpers.Configuration, options =>
                {
                    options.Seed = 42;
                })
                .Services
                .Configure<ChatOptions>(options =>
                {
                    options.Defaults = new ChatConfiguration
                    {
                        SystemMessages =
                        [
                            "You are a fiendly agent. Always use the result from the tool if you have called one.",
                            "Say hello to the user.",
                        ],
                    };
                    options.Configurations = new Dictionary<string, ChatConfiguration>
                    {
                        ["images"] = new ChatConfiguration
                        {
                            SystemMessages =
                            [
                                "You are a bot to generate images. Tell the user about your capabilities in a single, short sentence.",
                            ],
                        },
                        ["notool"] = new ChatConfiguration
                        {
                            Tools = [],
                        },
                    };
                })
                .BuildServiceProvider();

        var initializables = services.GetRequiredService<IEnumerable<IInitializable>>();

        foreach (var initializable in initializables)
        {
            await initializable.InitializeAsync(default);
        }

        return (services.GetRequiredService<IChatAgent>(), services);
    }

    private static void AssertMessage(string text, ChatResult message)
    {
        Assert.True(message.Metadata.CostsInEUR is > 0 and < 1);
        Assert.True(message.Metadata.NumInputTokens > 0);
        Assert.True(message.Metadata.NumOutputTokens > 0);
        Assert.Equal(Trim(text), Trim(message.Content));

        static string Trim(string text)
        {
            return text.Trim('"', '.');
        }
    }
}
