// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;
using Squidex.AI;
using Squidex.AI.SemanticKernel;

namespace Microsoft.Extensions.DependencyInjection;

public static class SementicKernelServiceExtensions
{
    public static IKernelBuilder AddTool<T>(this IKernelBuilder builder)
    {
        builder.Plugins.AddFromType<T>();
        return builder;
    }

    public static IServiceCollection AddOpenAIChatAgent(this IServiceCollection services, IConfiguration config, Action<OpenAIChatBotOptions>? configure = null,
        string configPath = "chatbot:openai")
    {
        services.Configure(config, configPath, configure);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IChatStore, InMemoryChatStore>();
        services.AddSingletonAs<OpenAIChatAgent>()
            .As<IChatAgent>().AsSelf();

        return services;
    }
}
