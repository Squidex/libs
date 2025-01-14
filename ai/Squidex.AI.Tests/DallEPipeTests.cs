// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Squidex.AI.Implementation.OpenAI;
using Squidex.AI.Utils;
using Squidex.Assets;
using Squidex.Assets.ImageSharp;
using Squidex.Hosting;
using Xunit;

namespace Squidex.AI;

public class DalLEPipeTests
{
    private readonly ChatContext context = new ChatContext();

    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_create_article_with_image()
    {
        var (sut, _) = await CreateSutAsync(downloadImage: false);

        var request1 = new ChatRequest
        {
            Prompt = "Write a short article about Paris. Add a single image.",
            ConversationId = string.Empty,
        };

        var message1 = await sut.PromptAsync(request1, context);
        Assert.Contains("](https://oaidalleapiprodscus.blob.core.windows.net", message1.Content, StringComparison.Ordinal);
    }

    private static async Task<(IChatAgent, IServiceProvider)> CreateSutAsync(bool downloadImage)
    {
        var services =
            new ServiceCollection()
                .AddSingleton<IHttpImageEndpoint, ImageEndpoint>()
                .AddSingleton<IAssetStore, MemoryAssetStore>()
                .AddSingleton<IAssetThumbnailGenerator, ImageSharpThumbnailGenerator>()
                .AddAI()
                .AddDallE(TestHelpers.Configuration, options =>
                {
                    options.DownloadImage = downloadImage;
                })
                .AddOpenAIChat(TestHelpers.Configuration, options =>
                {
                    options.Seed = 42;
                })
                .AddAIImagePipe()
                .Services
                .Configure<ChatOptions>(options =>
                {
                    options.Defaults = new ChatConfiguration
                    {
                        SystemMessages =
                        [
                            "You are a fiendly agent. Always use the result from the tool if you have called one.",
                            "When you are asked to generate content such as articles, add placeholders for image, describe and use the following pattern: <IMG>{description}</IMG>. {description} is the generated image description."
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
