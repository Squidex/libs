﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.AI.Implementation;
using Squidex.AI.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

public static class MongoChatServiceExtensions
{
    public static IServiceCollection AddMongoChatStore(this IServiceCollection services, IConfiguration config, Action<MongoChatStoreOptions>? configure = null,
        string configPath = "chatBot:mongoDb")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<MongoChatStore>()
            .As<IChatStore>();
        return services;
    }
}
