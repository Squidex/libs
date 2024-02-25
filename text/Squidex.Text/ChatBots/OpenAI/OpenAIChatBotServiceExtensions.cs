// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Squidex.Text.ChatBots;
using Squidex.Text.ChatBots.OpenAI;

namespace Microsoft.Extensions.DependencyInjection;

public static class OpenAIChatBotServiceExtensions
{
    public static IServiceCollection AddOpenAIChatAgent(this IServiceCollection services, IConfiguration config, Action<OpenAIChatBotOptions>? configure = null,
        string configPath = "chatbot:openai")
    {
        services.Configure(config, configPath, configure);

        services.TryAddSingleton<IChatStore, InMemoryChatStore>();
        services.AddSingletonAs<OpenAIChatAgent>()
            .As<IChatAgent>().AsSelf();

        return services;
    }
}
