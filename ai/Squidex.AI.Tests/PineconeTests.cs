// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Squidex.AI.Implementation.Pinecone;
using Squidex.Hosting;
using Xunit;

namespace Squidex.AI;

public class PineconeTests
{
    private readonly ChatContext context = new ChatContext();

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_answer_question()
    {
        var (sut, _) = await CreateSutAsync();

        var request1 = new ChatRequest
        {
            Prompt = "What is Squidex?",
            ConversationId = string.Empty,
        };

        var message = await sut.PromptAsync(request1, context);

        Assert.NotEmpty(message.Content);
        Assert.Contains(message.ToolEnds.Select(x => x.Tool), x => x is PineconeTool);
    }

    private static async Task<(IChatAgent, IServiceProvider)> CreateSutAsync()
    {
        var services =
            new ServiceCollection()
                .AddAI()
                .AddOpenAIChat(TestHelpers.Configuration, options =>
                {
                    options.Seed = 42;
                })
                .AddOpenAIEmbeddings(TestHelpers.Configuration)
                .AddPineconeTool(TestHelpers.Configuration, options =>
                {
                    options.ToolDescription = "Answers questions about Squidex.";
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
                })
                .BuildServiceProvider();

        var initializables = services.GetRequiredService<IEnumerable<IInitializable>>();

        foreach (var initializable in initializables)
        {
            await initializable.InitializeAsync(default);
        }

        return (services.GetRequiredService<IChatAgent>(), services);
    }
}
