// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.EntityFramework;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingServiceExtensions
{
    public static MessagingBuilder AddEntityFrameworkTransport<T>(this MessagingBuilder builder, IConfiguration config, Action<EFTransportOptions>? configure = null,
        string configPath = "messaging:sql") where T : DbContext
    {
        builder.Services.Configure(config, configPath, configure);

        builder.Services.AddSingletonAs<EFTransport<T>>()
            .As<IMessagingTransport>();
        builder.Services.AddSingleton(TimeProvider.System);

        return builder;
    }

    public static MessagingBuilder AddEntityFrameworkDataStore<T>(this MessagingBuilder builder, IConfiguration config, Action<EFMessagingDataStoreOptions>? configure = null,
        string configPath = "messaging:sql:subscriptions") where T : DbContext
    {
        builder.Services.Configure(config, configPath, configure);

        builder.Services.AddSingletonAs<EFMessagingDataStore<T>>()
            .As<IMessagingDataStore>();
        builder.Services.AddSingleton(TimeProvider.System);

        return builder;
    }
}
