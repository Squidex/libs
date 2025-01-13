// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Squidex.AI.Implementation;
using Squidex.AI.Implementation.OpenAI;
using Squidex.AI.Implementation.Pinecone;

namespace Squidex.AI;

public static class AIServiceExtensions
{
    public static AIBuilder AddAI(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddOptions<ChatOptions>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IChatStore, InMemoryChatStore>();
        services.TryAddSingleton<IChatAgent, ChatAgent>();
        services.TryAddSingleton<IChatProvider, NoopChatProvider>();

        return new AIBuilder(services);
    }

    public static AIBuilder AddCleaner(this AIBuilder builder)
    {
        builder.Services.AddSingletonAs<ChatCleaner>()
            .AsSelf();

        return builder;
    }

    public static AIBuilder AddAIPipe<T>(this AIBuilder builder) where T : class, IChatPipe
    {
        builder.Services.AddSingletonAs<T>()
            .As<IChatPipe>();

        return builder;
    }

    public static AIBuilder AddTool<T>(this AIBuilder builder) where T : class, IChatTool
    {
        builder.Services.AddSingletonAs<T>()
            .AsSelf();

        builder.Services.AddSingletonAs<SingleChatToolProvider<T>>()
            .As<IChatToolProvider>();

        return builder;
    }

    public static AIBuilder AddDallE(this AIBuilder builder, IConfiguration config, Action<DallEOptions>? configure = null,
        string configPath = "chatBot:dallE")
    {
        builder.Services.Configure(config, configPath, configure);

        builder.Services.AddSingletonAs<DallETool>()
            .AsSelf().As<IImageTool>();

        builder.Services.AddSingletonAs<SingleChatToolProvider<DallETool>>()
            .As<IChatToolProvider>();

        return builder;
    }

    public static AIBuilder AddAIImagePipe(this AIBuilder builder)
    {
        builder.AddAIPipe<ImagePipe>();

        return builder;
    }

    public static AIBuilder AddPineconeTool(this AIBuilder builder, IConfiguration config, Action<PineconeOptions>? configure = null,
        string configPath = "chatBot:pinecone")
    {
        builder.Services.Configure(config, configPath, configure);
        builder.AddTool<PineconeTool>();

        return builder;
    }

    public static AIBuilder AddOpenAIChat(this AIBuilder builder, IConfiguration config, Action<OpenAIChatOptions>? configure = null,
        string configPath = "chatBot:openai")
    {
        builder.Services.Configure(config, configPath, configure);

        builder.Services.AddSingletonAs<OpenAIChatProvider>()
            .As<IChatProvider>().AsSelf();

        return builder;
    }

    public static AIBuilder AddOpenAIEmbeddings(this AIBuilder builder, IConfiguration config, Action<OpenAIEmbeddingsOptions>? configure = null,
        string configPath = "chatBot:openaiEmbeddings")
    {
        builder.Services.Configure(config, configPath, configure);

        builder.Services.AddSingletonAs<OpenAIEmbeddings>()
            .As<IEmbeddings>().AsSelf();

        return builder;
    }
}
