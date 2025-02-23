// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.AI;
using Squidex.AI.Implementation;
using Squidex.AI.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

public static class MongoChatServiceExtensions
{
    public static AIBuilder AddMongoChatStore(this AIBuilder builder, IConfiguration config, Action<MongoChatStoreOptions>? configure = null,
        string configPath = "chatBot:mongoDb")
    {
        builder.Services.ConfigureAndValidate(config, configPath, configure);

        builder.Services.AddSingletonAs<MongoChatStore>()
            .As<IChatStore>();

        return builder;
    }
}
