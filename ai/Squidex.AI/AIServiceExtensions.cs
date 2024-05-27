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
    public static IServiceCollection AddAI(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddOptions<ChatOptions>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IChatStore, MemoryChatStore>();
        services.TryAddSingleton<IChatAgent, ChatAgent>();
        services.TryAddSingleton<IChatProvider, NoopChatProvider>();

        return services;
    }

    public static IServiceCollection AddAICleaner(this IServiceCollection services)
    {
        services.AddSingletonAs<ChatCleaner>()
            .AsSelf();

        return services;
    }

    public static IServiceCollection AddTool<T>(this IServiceCollection services) where T : class, IChatTool
    {
        services.AddSingletonAs<T>()
            .As<IChatTool>();

        return services;
    }

    public static IServiceCollection AddDallE(this IServiceCollection services, IConfiguration config, Action<DallEOptions>? configure = null,
        string configPath = "chatBot:dallE")
    {
        services.Configure(config, configPath, configure);

        services.AddAI();
        services.AddTool<DallETool>();

        return services;
    }

    public static IServiceCollection AddPineconeTool(this IServiceCollection services, IConfiguration config, Action<PineconeOptions>? configure = null,
        string configPath = "chatBot:pinecone")
    {
        services.Configure(config, configPath, configure);

        services.AddAI();
        services.AddTool<PineconeTool>();

        return services;
    }

    public static IServiceCollection AddOpenAIChat(this IServiceCollection services, IConfiguration config, Action<OpenAIChatOptions>? configure = null,
        string configPath = "chatBot:openai")
    {
        services.Configure(config, configPath, configure);

        services.AddAI();
        services.AddSingletonAs<OpenAIChatProvider>()
            .As<IChatProvider>().AsSelf();

        return services;
    }

    public static IServiceCollection AddOpenAIEmbeddings(this IServiceCollection services, IConfiguration config, Action<OpenAIEmbeddingsOptions>? configure = null,
        string configPath = "chatBot:openaiEmbeddings")
    {
        services.Configure(config, configPath, configure);

        services.AddAI();
        services.AddSingletonAs<OpenAIEmbeddings>()
            .As<IEmbeddings>().AsSelf();

        return services;
    }
}
