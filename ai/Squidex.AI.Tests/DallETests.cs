// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Markdig.Parsers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squidex.AI.Implementation;
using Squidex.AI.Implementation.OpenAI;
using Squidex.AI.Utils;
using Squidex.Assets;
using Squidex.Hosting;
using Xunit;

namespace Squidex.AI;

public class DalLETests
{
    private readonly ChatContext context = new ChatContext();

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_generate_image()
    {
        var (_, services) = await CreateSutAsync(downloadImage: false);

        var sut = services.GetRequiredService<IImageTool>();

        var ctx = new ToolContext
        {
            Arguments = new Dictionary<string, ToolValue>
            {
                ["query"] = new ToolStringValue("Puppy"),
            },
            ChatAgent = null!,
            Context = new ChatContext(),
            ToolData = []
        };

        var result = await sut.GenerateAsync(ctx, default);

        var markdownDoc = MarkdownParser.Parse(result);
        var markdownImage =
            markdownDoc.Descendants<ParagraphBlock>()
                .SelectMany(x => x.Inline!.Descendants<LinkInline>()).FirstOrDefault(l => l.IsImage);

        Assert.NotNull(markdownImage);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_create_dall_e_image()
    {
        var (sut, _) = await CreateSutAsync(downloadImage: false);

        var request1 = new ChatRequest
        {
            Prompt = "Create the image of a puppy.",
            ConversationId = string.Empty,
        };

        var message = await sut.PromptAsync(request1, context);

        AssertImageFromTool(message);
    }

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_create_dall_e_image_with_download()
    {
        var (sut, _) = await CreateSutAsync(downloadImage: true);

        var request1 = new ChatRequest
        {
            Prompt = "Create the image of a puppy.",
            ConversationId = string.Empty,
        };

        var message = await sut.PromptAsync(request1, context);

        AssertImageFromTool(message);
    }

    private static void AssertImageFromTool(ChatResult result)
    {
        var toolEnd = result.ToolEnds.Single();
        var toolJson = JsonDocument.Parse(toolEnd.Result);
        var toolUrl = toolJson.RootElement.GetProperty("url").ToString();

        var markdownDoc = MarkdownParser.Parse(result.Content);

        var markdownImage =
            markdownDoc.Descendants<ParagraphBlock>()
                .SelectMany(x => x.Inline!.Descendants<LinkInline>()).FirstOrDefault(l => l.IsImage);

        Assert.Equal(toolUrl, markdownImage?.Url);
        Assert.NotNull(markdownImage?.FirstChild);
        Assert.NotNull(markdownImage?.LastChild);
    }

    private static async Task<(IChatAgent, IServiceProvider)> CreateSutAsync(bool downloadImage)
    {
        var services =
            new ServiceCollection()
                .AddSingleton<IHttpImageEndpoint, ImageEndpoint>()
                .AddSingleton<IAssetStore, MemoryAssetStore>()
                .AddSingleton<IAssetThumbnailGenerator, ImageSharpThumbnailGenerator>()
                .AddDallE(TestHelpers.Configuration, options =>
                {
                    options.DownloadImage = downloadImage;
                })
                .AddOpenAIChat(TestHelpers.Configuration, options =>
                {
                    options.Seed = 42;
                })
                .Configure<ChatOptions>(options =>
                {
                    options.Defaults = new ChatConfiguration
                    {
                        SystemMessages =
                        [
                            "You are a fiendly agent. Always use the result from the tool if you have called one.",
                            "Say hello to the user."
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
