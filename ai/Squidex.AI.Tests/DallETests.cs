﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
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
    public async Task Should_create_dall_e_image()
    {
        var (sut, _) = await CreateSutAsync(downloadImage: false);

        var request1 = new ChatRequest
        {
            Prompt = "Create the image of a puppy.",
            ConversationId = string.Empty,
        };

        var message = await sut.PromptAsync(request1, context);
        Assert.Contains("https://", message.Content, StringComparison.Ordinal);
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
        Assert.Contains("https://", message.Content, StringComparison.Ordinal);
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
