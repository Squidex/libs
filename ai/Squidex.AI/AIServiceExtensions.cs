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

namespace Squidex.AI;

public static class AIServiceExtensions
{
    public static IServiceCollection AddAI(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddOptions<ChatOptions>();
        services.TryAddSingleton<IChatStore, MemoryChatStore>();
        services.TryAddSingleton<IChatAgent, ChatAgent>();
        services.TryAddSingleton<IChatProvider, NoopChatProvider>();

        return services;
    }

    public static IServiceCollection AddTool<T>(this IServiceCollection services) where T : class, IChatTool
    {
        services.AddSingleton<IChatTool, T>();

        return services;
    }

    public static IServiceCollection AddOpenAIChat(this IServiceCollection services, IConfiguration config, Action<OpenAIOptions>? configure = null,
        string configPath = "ai:openai")
    {
        services.Configure(config, configPath, configure);

        services.AddAI();
        services.AddSingletonAs<OpenAIChatProvider>()
            .As<IChatProvider>().AsSelf();

        return services;
    }
}
