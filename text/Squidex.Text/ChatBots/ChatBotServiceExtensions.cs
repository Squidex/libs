// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection.Extensions;
using Squidex.Text.ChatBots;

namespace Microsoft.Extensions.DependencyInjection;

public static class ChatBotServiceExtensions
{
    public static IServiceCollection AddChatBot(this IServiceCollection services)
    {
        services.TryAddSingleton<IChatBot, ChatBot>();

        return services;
    }
}
