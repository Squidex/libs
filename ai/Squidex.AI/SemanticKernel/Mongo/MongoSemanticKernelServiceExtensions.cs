// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Squidex.AI.SemanticKernel;
using Squidex.AI.SemanticKernel.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

public static class MongoSemanticKernelServiceExtensions
{
    public static IKernelBuilder AddMongoChatStore(this IKernelBuilder builder, IConfiguration config, Action<MongoChatStoreOptions>? configure = null,
        string configPath = "ai:mongoDb")
    {
        builder.Services.ConfigureAndValidate(config, configPath, configure);

        builder.Services.AddSingletonAs<MongoChatStore>()
            .As<IChatStore>();

        return builder;
    }
}
