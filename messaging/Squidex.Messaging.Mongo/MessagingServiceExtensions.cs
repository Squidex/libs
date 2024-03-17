// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.Mongo;
using Squidex.Messaging.Subscriptions;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingServiceExtensions
{
    public static IServiceCollection AddMongoTransport(this IServiceCollection services, IConfiguration config, Action<MongoTransportOptions>? configure = null,
        string configPath = "messaging:mongoDb")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<MongoTransport>()
            .As<IMessagingTransport>();

        return services;
    }

    public static IServiceCollection AddMongoMessagingData(this IServiceCollection services, IConfiguration config, Action<MongoMessagingDataOptions>? configure = null,
        string configPath = "messaging:mongoDb:subscriptions")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<MongoMessagingDataStore>()
            .As<IMessagingDataStore>();

        return services;
    }
}
